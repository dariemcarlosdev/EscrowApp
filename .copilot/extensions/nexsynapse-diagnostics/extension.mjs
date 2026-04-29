// NexSynapse Self-Diagnostics Extension
// Validates infrastructure health by checking manifest against reality.
// Returns CRITICAL / WARNING / INFO findings — not just a score.

import { joinSession } from "@github/copilot-sdk/extension";
import { readFile, access, readdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { homedir } from 'node:os';

const ROOT = resolve('.');
const MANIFEST_PATH = join(ROOT, 'NexSynapse', 'nexsynapse.manifest.json');

async function fileExists(p) {
  try { await access(p); return true; } catch { return false; }
}

async function sha256(filePath) {
  try {
    const content = await readFile(filePath);
    return createHash('sha256').update(content).digest('hex').toUpperCase();
  } catch { return null; }
}

async function loadManifest() {
  try {
    const raw = await readFile(MANIFEST_PATH, 'utf-8');
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

async function countSkillFiles(dir) {
  try {
    const items = await readdir(dir, { withFileTypes: true, recursive: true });
    return items.filter(i => i.isFile() && i.name === 'SKILL.md' && !i.parentPath?.includes('references')).length;
  } catch { return 0; }
}

async function runDiagnostics() {
  const findings = [];
  const add = (severity, category, message) => findings.push({ severity, category, message });

  const manifest = await loadManifest();
  if (!manifest) {
    add('CRITICAL', 'manifest', 'nexsynapse.manifest.json not found or invalid — cannot validate infrastructure');
    return findings;
  }
  add('INFO', 'manifest', `Loaded manifest v${manifest.version} — "${manifest.name}"`);

  const c = manifest.components;

  // Skills
  const skillCount = await countSkillFiles(join(ROOT, c.skills.basePath));
  if (skillCount === 0) add('CRITICAL', 'skills', `Skills directory empty or missing: ${c.skills.basePath}`);
  else if (skillCount !== c.skills.count) add('WARNING', 'skills', `Expected ${c.skills.count} skills, found ${skillCount}`);
  else add('INFO', 'skills', `All ${skillCount} skills present ✓`);

  // Bridges
  const bridgeCount = await countSkillFiles(join(ROOT, c.bridges.basePath));
  if (bridgeCount !== c.bridges.count) add('WARNING', 'bridges', `Expected ${c.bridges.count} bridges, found ${bridgeCount}`);
  else add('INFO', 'bridges', `All ${bridgeCount} Claude bridges present ✓`);

  if (skillCount > 0 && bridgeCount > 0 && skillCount !== bridgeCount) {
    add('WARNING', 'mapping', `Skill-bridge mismatch: ${skillCount} skills vs ${bridgeCount} bridges`);
  }

  // Extensions
  const extDir = join(ROOT, c.extensions.basePath);
  const extDirs = await readdir(extDir, { withFileTypes: true }).then(items => items.filter(i => i.isDirectory()).map(i => i.name)).catch(() => []);
  const missingExt = c.extensions.items.filter(e => !extDirs.includes(e));
  if (missingExt.length > 0) add('WARNING', 'extensions', `Missing extensions: ${missingExt.join(', ')}`);
  if (extDirs.length === c.extensions.count) add('INFO', 'extensions', `All ${c.extensions.count} extensions present ✓`);
  else add('WARNING', 'extensions', `Expected ${c.extensions.count} extensions, found ${extDirs.length}`);

  // Agents
  for (const agent of c.agents.items) {
    if (!await fileExists(join(ROOT, c.agents.basePath, `${agent}.md`))) add('WARNING', 'agents', `Agent missing: ${agent}.md`);
  }
  add('INFO', 'agents', `Checked ${c.agents.count} agents`);

  // Pre-commit hook
  const hookPath = join(ROOT, c.hooks.installed);
  if (!await fileExists(hookPath)) {
    add('CRITICAL', 'hooks', `Pre-commit hook NOT installed at ${c.hooks.installed}`);
  } else {
    const hookContent = await readFile(hookPath, 'utf-8').catch(() => '');
    const hasIp = hookContent.includes('CHECK 1') || hookContent.includes('IP_PATTERNS');
    const hasSecrets = hookContent.includes('CHECK 2') || hookContent.includes('SECRETS_PATTERNS');
    if (!hasIp) add('CRITICAL', 'hooks', 'Pre-commit hook missing IP path blocking (CHECK 1)');
    if (!hasSecrets) add('WARNING', 'hooks', 'Pre-commit hook missing secrets scan (CHECK 2)');
    if (hasIp && hasSecrets) add('INFO', 'hooks', 'Pre-commit hook installed with both checks ✓');
  }

  // .gitignore IP patterns
  const gitignorePath = join(ROOT, '.gitignore');
  if (await fileExists(gitignorePath)) {
    const gi = await readFile(gitignorePath, 'utf-8');
    const required = ['NexSynapse/', '.github/skills/', '.github/extensions/', '.claude/'];
    const missing = required.filter(p => !gi.split('\n').some(line => {
      const trimmed = line.trim();
      return trimmed === p || p.startsWith(trimmed.replace(/\/$/, '/'));
    }));
    if (missing.length > 0) add('CRITICAL', 'gitignore', `Missing IP patterns: ${missing.join(', ')}`);
    else add('INFO', 'gitignore', 'All critical .gitignore IP patterns present ✓');
  } else {
    add('CRITICAL', 'gitignore', '.gitignore file not found');
  }

  // Model bridges
  for (const bridge of c.modelBridges.items) {
    if (!await fileExists(join(ROOT, bridge))) add('WARNING', 'bridges', `Model bridge missing: ${bridge}`);
  }
  add('INFO', 'bridges', `Checked ${c.modelBridges.items.length} model bridge files`);

  // Tamper detection
  const baselines = manifest.protectedFiles?.baselines || {};
  let tamperOk = 0, tamperFail = 0;
  for (const [file, expectedHash] of Object.entries(baselines)) {
    const actual = await sha256(join(ROOT, file));
    if (!actual) { add('CRITICAL', 'integrity', `Protected file missing: ${file}`); tamperFail++; }
    else if (actual !== expectedHash) { add('WARNING', 'integrity', `Hash mismatch on ${file} — file modified since baseline`); tamperFail++; }
    else tamperOk++;
  }
  if (tamperFail === 0 && tamperOk > 0) add('INFO', 'integrity', `All ${tamperOk} protected files pass integrity check ✓`);

  // Identity files
  for (const f of ['NexSynapse/VERSION', 'NexSynapse/LICENSE', 'NexSynapse/CHANGELOG.md', 'NexSynapse/README.md']) {
    if (!await fileExists(join(ROOT, f))) add('WARNING', 'identity', `Missing: ${f}`);
  }
  add('INFO', 'identity', 'Identity files checked');

  // MemPalace
  const palaceDir = join(homedir(), '.mempalace');
  add('INFO', 'memory', await fileExists(palaceDir) ? 'MemPalace found at ~/.mempalace/ ✓' : 'MemPalace not installed (optional)');

  return findings;
}

function formatReport(findings) {
  const criticals = findings.filter(f => f.severity === 'CRITICAL');
  const warnings = findings.filter(f => f.severity === 'WARNING');
  const infos = findings.filter(f => f.severity === 'INFO');
  const total = findings.length;
  const passed = infos.length;
  const score = total > 0 ? Math.round((passed / total) * 100) : 0;

  let r = '# 🏥 NexSynapse Health Report\n\n';
  if (criticals.length > 0) {
    r += `## 🔴 CRITICAL (${criticals.length})\n\n`;
    criticals.forEach(f => { r += `- **[${f.category}]** ${f.message}\n`; });
    r += '\n';
  }
  if (warnings.length > 0) {
    r += `## 🟡 WARNING (${warnings.length})\n\n`;
    warnings.forEach(f => { r += `- **[${f.category}]** ${f.message}\n`; });
    r += '\n';
  }
  r += `## 🟢 PASSED (${infos.length})\n\n`;
  infos.forEach(f => { r += `- **[${f.category}]** ${f.message}\n`; });
  r += `\n---\n\n**Health Score: ${score}%** (${passed}/${total})\n\n`;
  if (criticals.length > 0) r += `⛔ **CRITICAL issues — infrastructure protection may be compromised.**\n`;
  else if (warnings.length > 0) r += `⚠️ **Warnings found — review recommended.**\n`;
  else r += `✅ **All systems healthy.**\n`;
  return r;
}

// ─── Trigger patterns ──────────────────────────────────────────────────────

const TRIGGERS = [
  /health\s*check/i, /diagnostic/i, /nexsynapse.*status/i,
  /infrastructure.*health/i, /check.*infrastructure/i, /verify.*nexsynapse/i,
  /self.?diagnos/i, /system.*check/i, /verificar.*infraestructura/i,
  /estado.*nexsynapse/i, /salud.*sistema/i,
];

// ─── Session ───────────────────────────────────────────────────────────────

const session = await joinSession({
  hooks: {
    onSessionStart: async () => {
      await session.log("🏥 NexSynapse Diagnostics extension loaded", { ephemeral: true });
    },

    onUserPromptSubmitted: async (input) => {
      const prompt = input.prompt;
      if (!prompt || typeof prompt !== "string") return;

      if (TRIGGERS.some(t => t.test(prompt))) {
        await session.log("🏥 Health check triggered", { ephemeral: true });
        return {
          additionalContext:
            "🏥 HEALTH CHECK TRIGGER DETECTED: The user is asking about NexSynapse infrastructure health. Call the `nexsynapse_health_check` tool to validate the manifest against reality. The tool checks: skills count, bridges mapping, extensions, agents, pre-commit hook, .gitignore IP patterns, model bridges, SHA-256 integrity baselines, identity files, and MemPalace. Zero cost — local filesystem only.",
        };
      }
    },
  },

  tools: [
    {
      name: "nexsynapse_health_check",
      description: "Run NexSynapse infrastructure health check — validates manifest, skills, extensions, hooks, security, and integrity baselines.",
      parameters: {
        type: "object",
        properties: {},
        additionalProperties: false,
      },
      handler: async () => formatReport(await runDiagnostics()),
    },
    {
      name: "nexsynapse_update_baselines",
      description: "Recompute SHA-256 baselines for protected files and update the manifest. Use after intentional modifications.",
      parameters: {
        type: "object",
        properties: {},
        additionalProperties: false,
      },
      handler: async () => {
        const manifest = await loadManifest();
        if (!manifest) return 'ERROR: manifest not found';
        const files = Object.keys(manifest.protectedFiles.baselines);
        const updated = {};
        for (const file of files) {
          const hash = await sha256(join(ROOT, file));
          updated[file] = hash || `MISSING — ${file}`;
        }
        manifest.protectedFiles.baselines = updated;
        const { writeFile } = await import('node:fs/promises');
        await writeFile(MANIFEST_PATH, JSON.stringify(manifest, null, 2) + '\n', 'utf-8');
        let report = '# Baselines Updated\n\n';
        for (const [file, hash] of Object.entries(updated)) {
          report += `${hash.startsWith('MISSING') ? '❌' : '✅'} \`${file}\` → \`${hash.slice(0, 16)}...\`\n`;
        }
        return report;
      },
    },
  ],
});
