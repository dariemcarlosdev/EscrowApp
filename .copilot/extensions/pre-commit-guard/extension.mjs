import { joinSession } from "@github/copilot-sdk/extension";
import { execSync } from "node:child_process";
import { readFileSync, existsSync, writeFileSync, mkdirSync, chmodSync } from "node:fs";
import { join, resolve } from "node:path";
import { homedir } from "node:os";

// ──────────────────────────────────────────────────────────
// Trigger patterns — detect commit/push/ship intent (EN + ES)
// ──────────────────────────────────────────────────────────
const COMMIT_TRIGGERS = [
  /\bgit\s+commit\b/i,
  /\bgit\s+push\b/i,
  /\bcommit\b.*\b(code|changes|files)\b/i,
  /\b(ready\s+to|want\s+to|going\s+to)\s+commit\b/i,
  /\b(ship|deploy|release)\s+(it|this|code|changes)\b/i,
  /\bpush\s+(to|this|my)\b/i,
  /\bsave.*\b(repo|repository|git)\b/i,
  /\bcheck\s+in\b/i,
  /\bmerge\b.*\b(pr|pull\s+request|branch)\b/i,
  /\bcommit(ear|ir)\b/i,              // Spanish: commitear/commitir
  /\benviar\s+(código|cambios)\b/i,    // Spanish: enviar código/cambios
  /\bsubir\s+(a|al)\s+(repo|git)\b/i,  // Spanish: subir al repo
  /\bpre.?commit\s+scan\b/i,
  /\bsecurity\s+(scan|check)\s+before\b/i,
  /\bscan\s+before\s+commit\b/i,
  /\bscan\s+staged\b/i,
];

// ──────────────────────────────────────────────────────────
// Security scan patterns — organized by severity
// ──────────────────────────────────────────────────────────
const CRITICAL_RULES = [
  { id: "CRIT-001", name: "Stripe Secret Key",           pattern: /sk_(live|test)_[a-zA-Z0-9]{20,}/g },
  { id: "CRIT-002", name: "GitHub Token",                pattern: /(ghp|gho|ghu|ghs|ghr)_[a-zA-Z0-9]{36,}/g },
  { id: "CRIT-003", name: "AWS Secret Key",              pattern: /(?:aws_secret_access_key|AKIA)[a-zA-Z0-9/+=]{20,}/gi },
  { id: "CRIT-004", name: "Hardcoded Password",          pattern: /(?:password|passwd|pwd)\s*[:=]\s*["'][^"']{4,}["']/gi },
  { id: "CRIT-005", name: "Connection String Password",  pattern: /(?:Password|PWD)\s*=\s*[^;"\s]{4,}/gi },
  { id: "CRIT-006", name: "Private Key",                 pattern: /-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----/g },
];

const HIGH_RULES = [
  { id: "HIGH-001", name: "Generic API Key",             pattern: /(?:api[_-]?key|apikey)\s*[:=]\s*["'][a-zA-Z0-9_\-]{16,}["']/gi },
  { id: "HIGH-002", name: "Bearer Token Hardcoded",      pattern: /["']Bearer\s+[a-zA-Z0-9._\-]{20,}["']/g },
  { id: "HIGH-003", name: "JWT Token",                   pattern: /eyJ[a-zA-Z0-9_-]{10,}\.eyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_\-]{10,}/g },
  { id: "HIGH-004", name: "Missing [Authorize]",         check: "structural" },
  { id: "HIGH-005", name: "SQL Injection Risk",          pattern: /(?:FromSqlRaw|ExecuteSqlRaw)\s*\(\s*\$"/gi },
  { id: "HIGH-006", name: "Crypto Key Material",         pattern: /(?:secret|signing[_-]?key)\s*[:=]\s*["'][a-zA-Z0-9+/=]{16,}["']/gi },
  { id: "HIGH-007", name: "Webhook Secret Hardcoded",    pattern: /whsec_[a-zA-Z0-9]{20,}/g },
];

const MEDIUM_RULES = [
  { id: "MED-001",  name: "TODO Security",               pattern: /TODO.*(?:security|auth|encrypt|secret|password)/gi },
  { id: "MED-002",  name: "Console.Write in Production", pattern: /Console\.Write(?:Line)?\s*\(/g },
  { id: "MED-003",  name: "Disabled SSL Validation",     pattern: /ServerCertificateCustomValidationCallback\s*=.*=>.*true/g },
];

// File extensions to scan
const SCANNABLE_EXTENSIONS = [".cs", ".razor", ".json", ".yaml", ".yml", ".xml", ".config", ".env", ".csproj"];
const SKIP_DIRS = ["bin", "obj", "node_modules", ".git", "wwwroot/lib"];

// ──────────────────────────────────────────────────────────
// Core scan logic
// ──────────────────────────────────────────────────────────

function getRepoRoot() {
  try {
    return execSync("git rev-parse --show-toplevel", { encoding: "utf-8" }).trim().replace(/\//g, "\\");
  } catch {
    return process.cwd();
  }
}

function getStagedFiles() {
  try {
    const output = execSync("git diff --cached --name-only --diff-filter=ACMR", { encoding: "utf-8" }).trim();
    return output ? output.split("\n").map(f => f.trim()) : [];
  } catch {
    return [];
  }
}

function getModifiedFiles() {
  try {
    const output = execSync("git diff --name-only --diff-filter=ACMR", { encoding: "utf-8" }).trim();
    const staged = execSync("git diff --cached --name-only --diff-filter=ACMR", { encoding: "utf-8" }).trim();
    const all = [output, staged].filter(Boolean).join("\n");
    return all ? [...new Set(all.split("\n").map(f => f.trim()))] : [];
  } catch {
    return [];
  }
}

function getAllFiles(dir, files = []) {
  try {
    const entries = execSync(`Get-ChildItem -Path "${dir}" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName`, {
      encoding: "utf-8", shell: "powershell.exe", timeout: 15000
    }).trim();
    if (entries) {
      return entries.split("\n")
        .map(f => f.trim())
        .filter(f => {
          const ext = f.substring(f.lastIndexOf(".")).toLowerCase();
          return SCANNABLE_EXTENSIONS.includes(ext) && !SKIP_DIRS.some(d => f.includes(`\\${d}\\`));
        });
    }
  } catch { /* fallback empty */ }
  return files;
}

function scanFile(filePath, repoRoot) {
  const findings = [];
  const fullPath = filePath.includes(":\\") ? filePath : join(repoRoot, filePath);

  if (!existsSync(fullPath)) return findings;

  let content;
  try {
    content = readFileSync(fullPath, "utf-8");
  } catch {
    return findings;
  }

  const lines = content.split("\n");
  const relPath = fullPath.replace(repoRoot + "\\", "").replace(/\\/g, "/");

  // Pattern-based rules
  const allRules = [
    ...CRITICAL_RULES.map(r => ({ ...r, severity: "🔴 CRITICAL" })),
    ...HIGH_RULES.filter(r => r.pattern).map(r => ({ ...r, severity: "🟠 HIGH" })),
    ...MEDIUM_RULES.map(r => ({ ...r, severity: "🟡 MEDIUM" })),
  ];

  for (const rule of allRules) {
    for (let i = 0; i < lines.length; i++) {
      rule.pattern.lastIndex = 0;
      if (rule.pattern.test(lines[i])) {
        // Skip comments and known safe patterns
        const trimmed = lines[i].trim();
        if (trimmed.startsWith("//") && !trimmed.includes("=")) continue;
        if (trimmed.startsWith("*") || trimmed.startsWith("<!--")) continue;
        // Skip pattern definitions in this extension itself
        if (relPath.includes("pre-commit-guard/extension.mjs")) continue;
        if (relPath.includes("security-scanner/extension.mjs")) continue;
        if (relPath.includes(".github/skills/")) continue;
        if (relPath.includes(".github/hooks/")) continue;

        findings.push({
          severity: rule.severity,
          id: rule.id,
          name: rule.name,
          file: relPath,
          line: i + 1,
          snippet: lines[i].trim().substring(0, 80),
        });
      }
    }
  }

  // Structural check: missing [Authorize] on pages/controllers
  const ext = fullPath.substring(fullPath.lastIndexOf(".")).toLowerCase();

  if (ext === ".razor") {
    const hasPage = content.includes("@page ");
    const hasAuth = content.includes("[Authorize") || content.includes("@attribute [Authorize");
    const hasAllowAnon = content.includes("[AllowAnonymous]");
    if (hasPage && !hasAuth && !hasAllowAnon) {
      findings.push({
        severity: "🟠 HIGH",
        id: "HIGH-004",
        name: "Missing [Authorize] on Blazor page",
        file: relPath,
        line: 1,
        snippet: "@page directive without [Authorize] attribute",
      });
    }
  }

  if (ext === ".cs" && content.includes("[ApiController]")) {
    const hasAuth = content.includes("[Authorize");
    if (!hasAuth) {
      findings.push({
        severity: "🟠 HIGH",
        id: "HIGH-004",
        name: "Missing [Authorize] on API controller",
        file: relPath,
        line: 1,
        snippet: "[ApiController] without [Authorize] attribute",
      });
    }
  }

  return findings;
}

function formatFindings(findings, scope) {
  if (findings.length === 0) {
    return `✅ **Pre-Commit Security Scan — CLEAN**\n\nScope: ${scope}\nNo security issues found. Safe to commit.\n`;
  }

  const critical = findings.filter(f => f.severity.includes("CRITICAL"));
  const high = findings.filter(f => f.severity.includes("HIGH"));
  const medium = findings.filter(f => f.severity.includes("MEDIUM"));

  let report = `🛡️ **Pre-Commit Security Scan Results**\n\n`;
  report += `Scope: ${scope}\n`;
  report += `Found: ${critical.length} CRITICAL | ${high.length} HIGH | ${medium.length} MEDIUM\n\n`;

  if (critical.length > 0) {
    report += `⛔ **COMMIT BLOCKED** — ${critical.length} CRITICAL finding(s) must be fixed first.\n\n`;
  }

  report += `| Severity | ID | Issue | File | Line |\n`;
  report += `|----------|-----|-------|------|------|\n`;

  for (const f of [...critical, ...high, ...medium]) {
    report += `| ${f.severity} | ${f.id} | ${f.name} | \`${f.file}\` | ${f.line} |\n`;
  }

  report += `\n### Remediation\n\n`;
  if (critical.length > 0) {
    report += `**CRITICAL fixes required before commit:**\n`;
    for (const f of critical) {
      report += `- **${f.id}** (${f.name}): Remove hardcoded secret from \`${f.file}:${f.line}\`. Use environment variables or Key Vault.\n`;
    }
    report += `\n`;
  }
  if (high.length > 0) {
    report += `**HIGH priority:**\n`;
    for (const f of high) {
      if (f.id === "HIGH-004") {
        report += `- **${f.id}**: Add \`[Authorize]\` or \`@attribute [Authorize]\` to \`${f.file}\`.\n`;
      } else if (f.id === "HIGH-005") {
        report += `- **${f.id}**: Replace \`FromSqlRaw\` with \`FromSqlInterpolated\` in \`${f.file}:${f.line}\`.\n`;
      } else {
        report += `- **${f.id}** (${f.name}): Review \`${f.file}:${f.line}\` — remove or externalize.\n`;
      }
    }
  }

  report += `\n> 💡 Use \`pre_commit_security_scan\` with mode "all" for a full codebase scan.\n`;
  report += `> 📖 Read \`.github/skills/security/pre-commit-guard/SKILL.md\` for the complete workflow.\n`;

  return report;
}

// ──────────────────────────────────────────────────────────
// Git hook installer
// ──────────────────────────────────────────────────────────

function getHookScript() {
  return `#!/bin/sh
# NexSynapse Pre-Commit Security Guard
# Installed by: pre-commit-guard extension
# Blocks commits containing CRITICAL security issues (hardcoded secrets)
# Bypass: git commit --no-verify

echo "🛡️  NexSynapse Pre-Commit Security Guard"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACMR | grep -E '\\.(cs|razor|json|yaml|yml|xml|config|env|csproj)$')

if [ -z "$STAGED_FILES" ]; then
  echo "✅ No scannable files staged. Proceeding."
  exit 0
fi

FOUND_ISSUES=0

for FILE in $STAGED_FILES; do
  if [ ! -f "$FILE" ]; then
    continue
  fi

  # CRIT-001: Stripe secret keys
  if grep -qE 'sk_(live|test)_[a-zA-Z0-9]{20,}' "$FILE"; then
    echo "⛔ CRIT-001: Stripe secret key found in $FILE"
    FOUND_ISSUES=1
  fi

  # CRIT-002: GitHub tokens
  if grep -qE '(ghp|gho|ghu|ghs|ghr)_[a-zA-Z0-9]{36,}' "$FILE"; then
    echo "⛔ CRIT-002: GitHub token found in $FILE"
    FOUND_ISSUES=1
  fi

  # CRIT-003: AWS secret keys
  if grep -qiE '(aws_secret_access_key|AKIA)[a-zA-Z0-9/+=]{20,}' "$FILE"; then
    echo "⛔ CRIT-003: AWS secret key found in $FILE"
    FOUND_ISSUES=1
  fi

  # CRIT-004: Hardcoded passwords
  if grep -qiE '(password|passwd|pwd)[[:space:]]*[:=][[:space:]]*["\x27][^\x27"]{4,}["\x27]' "$FILE"; then
    echo "⛔ CRIT-004: Hardcoded password found in $FILE"
    FOUND_ISSUES=1
  fi

  # CRIT-005: Connection string passwords
  if grep -qiE '(Password|PWD)[[:space:]]*=[[:space:]]*[^;"[:space:]]{4,}' "$FILE"; then
    echo "⛔ CRIT-005: Connection string password found in $FILE"
    FOUND_ISSUES=1
  fi

  # CRIT-006: Private keys
  if grep -qE 'BEGIN[[:space:]]+(RSA[[:space:]]+)?PRIVATE[[:space:]]+KEY' "$FILE"; then
    echo "⛔ CRIT-006: Private key found in $FILE"
    FOUND_ISSUES=1
  fi

  # HIGH-005: SQL injection risk
  if grep -qiE '(FromSqlRaw|ExecuteSqlRaw)' "$FILE"; then
    echo "⚠️  HIGH-005: Potential SQL injection (FromSqlRaw) in $FILE"
  fi
done

if [ $FOUND_ISSUES -eq 1 ]; then
  echo ""
  echo "⛔ COMMIT BLOCKED — CRITICAL security issues found."
  echo "   Fix the issues above or use 'git commit --no-verify' to bypass."
  echo "   Run the full scan: use pre_commit_security_scan tool"
  exit 1
fi

echo "✅ Pre-commit security scan passed. Proceeding with commit."
exit 0
`;
}

// ──────────────────────────────────────────────────────────
// Session setup — hooks + tools
// ──────────────────────────────────────────────────────────

const session = await joinSession();
session.log("Pre-Commit Security Guard loaded — cross-platform (Copilot CLI, Claude, Codex, Gemini)");

// ──────────────────────────────────────────────────────────
// Hook: Auto-detect commit/push intent
// ──────────────────────────────────────────────────────────
session.on("onUserPromptSubmitted", async ({ userPrompt }) => {
  const triggered = COMMIT_TRIGGERS.some(rx => rx.test(userPrompt));
  if (!triggered) return {};

  session.log("🛡️ Commit intent detected — injecting security scan reminder");

  return {
    additionalContext: [
      "🛡️ **NexSynapse Pre-Commit Security Guard**",
      "",
      "Commit/push intent detected. Before proceeding:",
      "1. Run the `pre_commit_security_scan` tool with mode 'staged' to scan staged files",
      "2. Fix any CRITICAL findings before committing",
      "3. If the git pre-commit hook is not installed, run `install_pre_commit_hook` to set it up",
      "",
      "This guard works across all AI models: Copilot CLI (extension), Claude/Codex/Gemini (skill at `.github/skills/security/pre-commit-guard/SKILL.md`), and native git (hook at `.git/hooks/pre-commit`).",
    ].join("\n"),
  };
});

// ──────────────────────────────────────────────────────────
// Hook: Intercept git commit/push commands
// ──────────────────────────────────────────────────────────
session.on("onPreToolUse", async ({ toolName, toolInput }) => {
  if (toolName !== "powershell") return {};

  const cmd = toolInput?.command || "";
  const isCommit = /\bgit\s+(commit|push)\b/i.test(cmd);
  const hasNoVerify = /--no-verify/.test(cmd);

  if (!isCommit || hasNoVerify) return {};

  session.log("🛡️ Intercepted git commit/push — scanning staged files first");

  const repoRoot = getRepoRoot();
  const stagedFiles = getStagedFiles();

  if (stagedFiles.length === 0) {
    return {
      additionalContext: "✅ No staged files to scan. Proceeding with commit.",
    };
  }

  const allFindings = [];
  for (const file of stagedFiles) {
    const findings = scanFile(file, repoRoot);
    allFindings.push(...findings);
  }

  const critical = allFindings.filter(f => f.severity.includes("CRITICAL"));

  if (critical.length > 0) {
    return {
      additionalContext: [
        "⛔ **SECURITY GATE — CRITICAL issues found in staged files!**",
        "",
        formatFindings(allFindings, `${stagedFiles.length} staged file(s)`),
        "",
        "**Do NOT proceed with the commit until CRITICAL issues are resolved.**",
        "To bypass (not recommended): add `--no-verify` flag.",
      ].join("\n"),
    };
  }

  if (allFindings.length > 0) {
    return {
      additionalContext: [
        "⚠️ **Pre-commit scan found non-critical issues:**",
        "",
        formatFindings(allFindings, `${stagedFiles.length} staged file(s)`),
        "",
        "These are warnings — commit can proceed, but consider fixing HIGH issues.",
      ].join("\n"),
    };
  }

  return {
    additionalContext: `✅ Pre-commit scan clean — ${stagedFiles.length} staged file(s) scanned, no issues found.`,
  };
});

// ──────────────────────────────────────────────────────────
// Tool: pre_commit_security_scan
// ──────────────────────────────────────────────────────────
session.on("tool", {
  name: "pre_commit_security_scan",
  description: "Scan files for security issues before committing. Checks for hardcoded secrets, missing [Authorize], SQL injection, and more. Zero API cost — runs locally.",
  parameters: {
    type: "object",
    properties: {
      mode: {
        type: "string",
        enum: ["staged", "modified", "all"],
        description: "Scope: 'staged' = git staged files only (default), 'modified' = all uncommitted changes, 'all' = full codebase scan",
        default: "staged",
      },
    },
  },
  handler: async ({ mode = "staged" }) => {
    const repoRoot = getRepoRoot();
    let filesToScan = [];
    let scopeLabel = "";

    switch (mode) {
      case "staged":
        filesToScan = getStagedFiles();
        scopeLabel = `${filesToScan.length} staged file(s)`;
        break;
      case "modified":
        filesToScan = getModifiedFiles();
        scopeLabel = `${filesToScan.length} modified file(s)`;
        break;
      case "all":
        filesToScan = getAllFiles(repoRoot);
        scopeLabel = `${filesToScan.length} file(s) in codebase`;
        break;
    }

    if (filesToScan.length === 0) {
      return `✅ No files to scan (mode: ${mode}). ${mode === "staged" ? "Stage files with 'git add' first." : "No matching files found."}`;
    }

    const allFindings = [];
    let scannedCount = 0;

    for (const file of filesToScan) {
      const findings = scanFile(file, repoRoot);
      allFindings.push(...findings);
      scannedCount++;
    }

    return formatFindings(allFindings, `${scopeLabel} (${scannedCount} scanned)`);
  },
});

// ──────────────────────────────────────────────────────────
// Tool: install_pre_commit_hook
// ──────────────────────────────────────────────────────────
session.on("tool", {
  name: "install_pre_commit_hook",
  description: "Install the NexSynapse pre-commit security hook into .git/hooks/pre-commit. Works with Git Bash on Windows and native shell on macOS/Linux. Zero cost — native git hook.",
  parameters: {
    type: "object",
    properties: {
      source: {
        type: "string",
        enum: ["embedded", "file"],
        description: "'embedded' (default) = write hook from extension, 'file' = copy from .github/hooks/pre-commit",
        default: "embedded",
      },
    },
  },
  handler: async ({ source = "embedded" }) => {
    const repoRoot = getRepoRoot();
    const hookDir = join(repoRoot, ".git", "hooks");
    const hookPath = join(hookDir, "pre-commit");

    // Check if .git exists
    if (!existsSync(join(repoRoot, ".git"))) {
      return "❌ Not a git repository. Run `git init` first.";
    }

    // Check for existing hook
    if (existsSync(hookPath)) {
      try {
        const existing = readFileSync(hookPath, "utf-8");
        if (existing.includes("NexSynapse Pre-Commit Security Guard")) {
          return "✅ NexSynapse pre-commit hook is already installed.";
        }
      } catch { /* proceed with overwrite warning */ }
      return "⚠️ A pre-commit hook already exists. To replace it, delete `.git/hooks/pre-commit` first, then run this tool again.";
    }

    // Ensure hooks directory exists
    if (!existsSync(hookDir)) {
      mkdirSync(hookDir, { recursive: true });
    }

    let hookContent;
    if (source === "file") {
      const sourceFile = join(repoRoot, ".github", "hooks", "pre-commit");
      if (!existsSync(sourceFile)) {
        return "❌ Source file `.github/hooks/pre-commit` not found. Use source='embedded' instead.";
      }
      hookContent = readFileSync(sourceFile, "utf-8");
    } else {
      hookContent = getHookScript();
    }

    writeFileSync(hookPath, hookContent, { encoding: "utf-8" });

    // Make executable on non-Windows
    try {
      chmodSync(hookPath, 0o755);
    } catch { /* Windows doesn't need chmod — Git Bash handles it */ }

    return [
      "✅ **NexSynapse Pre-Commit Security Hook Installed**",
      "",
      `Location: \`${hookPath}\``,
      "",
      "What it does:",
      "- Scans staged .cs, .razor, .json, .yaml, .xml files",
      "- Blocks commit on CRITICAL findings (hardcoded secrets, private keys)",
      "- Warns on HIGH findings (SQL injection risk)",
      "- Bypass: `git commit --no-verify`",
      "",
      "This hook works independently of any AI model — it's native git.",
      "The extension provides richer scanning via `pre_commit_security_scan` tool.",
    ].join("\n");
  },
});
