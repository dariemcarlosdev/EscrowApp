---
name: pre-commit-guard
description: "Scan code for security vulnerabilities before committing. Catches hardcoded secrets, missing authorization, SQL injection risks, and OWASP Top 10 violations at the earliest point."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  version: "2.0.0"
  domain: security
  triggers: pre-commit, security scan, before commit, check security, scan staged
  role: security-scanner
  scope: prevention
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: owasp-audit, secret-scanner, threat-modeler
---

# Pre-Commit Security Guard

> **Universal skill** — works across Copilot CLI, Claude, Codex, Gemini, and any AI assistant that can read files.
> Part of the NexSynapse portable AI infrastructure.

## Purpose

Scan code for security vulnerabilities **before** committing to the repository. Catches hardcoded secrets, missing authorization, SQL injection risks, and other OWASP Top 10 violations at the earliest possible point — before they enter version control.

## Three Layers of Protection

| Layer | Where | Works With | Catches |
|-------|-------|-----------|---------|
| **Copilot CLI Extension** | `.github/extensions/pre-commit-guard/extension.mjs` | Copilot CLI | Full scan: CRITICAL + HIGH + MEDIUM + structural |
| **Git Pre-Commit Hook** | `.github/hooks/pre-commit` → `.git/hooks/pre-commit` | Any AI model, native git | CRITICAL only: secrets, private keys |
| **This Skill** | `.github/skills/security/pre-commit-guard/SKILL.md` | Claude, Codex, Gemini, any model | Full workflow: scan → triage → fix → verify |

## When to Use

- **Before any `git commit`** — scan staged files for security issues
- **Before any `git push`** — final check before code reaches remote
- **During code review** — verify no secrets or auth gaps in changed files
- **On demand** — periodic full-codebase security sweep

---

## Core Workflow

### Step 1: Identify Files to Scan

Determine the scope based on the situation:

```bash
# Staged files (most common — pre-commit)
git diff --cached --name-only --diff-filter=ACMR

# All modified files (pre-push or review)
git diff --name-only --diff-filter=ACMR

# Full codebase scan
find . -type f \( -name "*.cs" -o -name "*.razor" -o -name "*.json" -o -name "*.yaml" -o -name "*.yml" \) -not -path "*/bin/*" -not -path "*/obj/*" -not -path "*/.git/*"
```

Filter to scannable extensions: `.cs`, `.razor`, `.json`, `.yaml`, `.yml`, `.xml`, `.config`, `.env`, `.csproj`

✅ **Checkpoint:** You have a list of files to scan.

---

### Step 2: Scan for Security Issues

Apply patterns from the **Reference Guide** below. Prioritize by severity:

**🔴 CRITICAL (blocks commit):**
- CRIT-001: Stripe secret keys (`sk_live_*`, `sk_test_*`)
- CRIT-002: GitHub tokens (`ghp_*`, `gho_*`, etc.)
- CRIT-003: AWS secret keys
- CRIT-004: Hardcoded passwords in assignments
- CRIT-005: Connection string passwords
- CRIT-006: Private keys (PEM format)

**🟠 HIGH (should fix before commit):**
- HIGH-001: Generic API keys in string literals
- HIGH-002: Hardcoded Bearer tokens
- HIGH-003: JWT tokens in source
- HIGH-004: Missing `[Authorize]` on Blazor `@page` or `[ApiController]`
- HIGH-005: `FromSqlRaw`/`ExecuteSqlRaw` with string interpolation (SQL injection)
- HIGH-006: Crypto key material in code
- HIGH-007: Stripe webhook secrets (`whsec_*`)

**🟡 MEDIUM (fix before merge):**
- MED-001: TODO comments mentioning security/auth
- MED-002: `Console.Write` in production code
- MED-003: Disabled SSL validation

✅ **Checkpoint:** All files scanned, findings listed with severity.

---

### Step 3: Triage Findings

For each finding, determine:

1. **Is it a real vulnerability or a false positive?**
   - Skip findings in comments, documentation, test mocks, and pattern definitions
   - Skip findings in `.github/skills/`, `.github/extensions/`, `.github/hooks/` (security pattern definitions)
   
2. **Is it in scope for this commit?**
   - Pre-existing issues in unchanged files are tracked but don't block

3. **What's the remediation?**
   - CRITICAL secrets → Move to environment variables, Key Vault, or `dotnet user-secrets`
   - Missing `[Authorize]` → Add `@attribute [Authorize]` or `[Authorize(Policy = "...")]`
   - SQL injection → Replace `FromSqlRaw` with `FromSqlInterpolated`
   - Hardcoded URLs → Move to configuration via `IOptions<T>`

✅ **Checkpoint:** Findings triaged — real issues identified, false positives dismissed.

---

### Step 4: Fix Issues

Apply remediation for each confirmed finding:

```csharp
// CRIT: Move secrets to configuration
// ❌ Before
"SecretKey": "sk_test_abc123..."

// ✅ After (use environment variable reference)
"SecretKey": "${STRIPE_SECRET_KEY}"

// HIGH-004: Add authorization
// ❌ Before
@page "/dashboard"

// ✅ After
@page "/dashboard"
@attribute [Authorize]

// HIGH-005: Fix SQL injection
// ❌ Before
context.FromSqlRaw($"SELECT * FROM t WHERE id = '{id}'")

// ✅ After
context.FromSqlInterpolated($"SELECT * FROM t WHERE id = {id}")
```

✅ **Checkpoint:** All CRITICAL and HIGH findings fixed.

---

### Step 5: Re-Scan and Commit

1. Re-scan the modified files to verify fixes
2. Stage the fixed files: `git add <files>`
3. Commit with clean scan

If the git pre-commit hook is installed, it will automatically run the CRITICAL-level scan.

✅ **Checkpoint:** Clean scan, commit successful.

---

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| **Scan Rules** | `references/scan-rules.md` | Need detailed patterns, regex, or remediation guidance |

---

## Installation

### Git Hook (works with ALL AI models)

```bash
# Copy hook to git directory
cp .github/hooks/pre-commit .git/hooks/pre-commit

# Make executable (macOS/Linux)
chmod +x .git/hooks/pre-commit

# Windows: Git Bash handles permissions automatically
```

### Copilot CLI Extension (automatic)

The extension at `.github/extensions/pre-commit-guard/extension.mjs` loads automatically. It provides:
- Auto-detection of commit/push intent (16+ trigger patterns)
- `pre_commit_security_scan` tool (staged/modified/all modes)
- `install_pre_commit_hook` tool (installs git hook)
- `onPreToolUse` hook that intercepts `git commit`/`git push` commands

### Claude / Codex / Gemini (skill)

Read this file and follow the Core Workflow. Skills are plain markdown — any model can read them.

```bash
# Claude
cat .github/skills/security/pre-commit-guard/SKILL.md

# Codex
cat .github/skills/security/pre-commit-guard/SKILL.md

# Gemini
cat .github/skills/security/pre-commit-guard/SKILL.md
```

---

## Cross-Platform Compatibility

| Platform | Layer | How It Works |
|----------|-------|-------------|
| **Copilot CLI** | Extension + Hook | Auto-detect hook, `onPreToolUse` intercept, tools, git hook |
| **Claude Code** | Skill bridge + Hook | `.claude/skills/pre-commit-guard/SKILL.md` → this file + git hook |
| **Codex CLI** | CODEX.md + Skill + Hook | CODEX.md references skill, reads this file + git hook |
| **Gemini** | GEMINI.md + Skill + Hook | GEMINI.md references skill, reads this file + git hook |
| **GitHub Actions** | Hook (CI) | Git hook runs in CI if configured; or add a workflow step |
| **Any AI model** | Skill + Hook | Read `.github/skills/security/pre-commit-guard/SKILL.md` + git hook |
