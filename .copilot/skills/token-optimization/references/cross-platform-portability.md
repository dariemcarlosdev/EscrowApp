# Cross-Platform Portability — Token-Optimized

> Reference for: `.github/skills/workflow/token-optimization/SKILL.md`
> Load when: Ensuring artifacts work across Claude, Codex, Gemini, and Copilot CLI.

---

## Platform Capability Matrix

| Capability | Copilot CLI | Claude Code | Gemini (CLI) | Codex CLI |
|------------|-------------|-------------|--------------|-----------|
| **Instruction files** | Custom instructions (repo settings) | `CLAUDE.md` + `.claude/` | `GEMINI.md` + `.agent/` | `CODEX.md` + `.codex/` |
| **Skills directory** | `.github/skills/` (native) | `.claude/skills/` (bridge → universal) | `.agent/rules/` + `.agent/workflows/` | Via AGENTS.md catalog ref |
| **Context loading** | Custom instructions auto-loaded | CLAUDE.md auto-loaded | GEMINI.md + rules auto-loaded | CODEX.md + AGENTS.md auto-loaded |
| **Scoped instructions** | `applyTo` glob patterns | `applyTo` glob patterns | `.agent/rules/` per-concern files | Protected paths in config.toml |
| **Skill discovery** | CATALOG.md + skill tool | `/skills` command | Read `.agent/rules/` | Read AGENTS.md skills section |
| **Max context** | ~200K tokens | ~200K tokens | ~1M tokens (Gemini 2.5) | ~128K tokens |
| **Config format** | N/A | `.claude/settings.json` | `.gemini/settings.json` | `.codex/config.toml` |

---

## Universal Source of Truth

All skills live in `.github/skills/` — this is the **single source of truth**.

```
.github/skills/                        ← Universal (all platforms read this)
├── CATALOG.md                         ← Master index
├── {category}/{skill}/SKILL.md        ← Core workflow
└── {category}/{skill}/references/     ← Lazy-loaded deep dives
```

Platform-specific files are **bridges only** — they point to the universal skill, never duplicate it.

```
.claude/skills/{name}/SKILL.md         ← Bridge → .github/skills/...
.agent/rules/{name}.md                 ← Condensed rules → .github/skills/...
.codex/README.md                       ← References AGENTS.md which has skills catalog
```

**Why:** One change to the universal file updates all platforms. No sync drift.

---

## Per-Platform Authoring Guide

### Copilot CLI

**Auto-loaded:** Custom instructions defined in repo settings (`.github/copilot-instructions.md` or equivalent). Also reads `applyTo`-scoped instruction blocks.

**Skills:** Reads `.github/skills/` natively. Register in CATALOG.md for discovery.

**Token strategy:**
- Copilot CLI loads custom instructions + scoped instructions on every turn
- Keep scoped instructions focused — one concern per `applyTo` block
- Skills loaded on-demand only when triggered

**Portability action:** No bridge file needed. Skill works natively.

### Claude Code

**Auto-loaded:** `CLAUDE.md` + all `.claude/settings.json` + AGENTS.md.

**Skills:** `.claude/skills/{name}/SKILL.md` appears in `/skills` list. Bridge files redirect to universal.

**Token strategy:**
- CLAUDE.md is loaded every session — keep it under 10 KB
- Use bridge files (< 1 KB) that redirect to `.github/skills/`
- Scoped instructions via `applyTo` reduce per-turn context

**Portability action:** Create `.claude/skills/{name}/SKILL.md` bridge file (~30 lines).

### Gemini CLI

**Auto-loaded:** `GEMINI.md` + all files in `.agent/rules/` + `.agent/workflows/`.

**Skills:** `.agent/rules/{name}.md` files are loaded as declarative rules.

**Token strategy:**
- Gemini has a massive context window (~1M) but rules are loaded eagerly
- Keep `.agent/rules/` files small (< 2 KB each) — they're ALL loaded
- Use `.agent/workflows/` for procedural multi-step processes
- Reference `.github/skills/` for full detail; rules are the condensed version

**Portability action:** Create `.agent/rules/{name}.md` condensed rule file (~40 lines).

**⚠️ Gemini gotcha:** Unlike Claude and Copilot, Gemini loads ALL rules files into context on every session. Each rule file should be the minimal, declarative subset — not the full skill.

### Codex CLI

**Auto-loaded:** `AGENTS.md` (primary) + `CODEX.md` (fallback, per config.toml).

**Skills:** Codex reads AGENTS.md which contains the Skills Catalog section pointing to `.github/skills/CATALOG.md`.

**Token strategy:**
- Codex has the smallest default context (128K) — most token-sensitive platform
- `config.toml` sets `max_bytes = 32768` for instruction files
- Keep CODEX.md focused on Codex-specific behavior only
- Skills are NOT auto-loaded — agent must read CATALOG.md then read the specific skill file

**Portability action:** Ensure AGENTS.md Skills Catalog section is up-to-date. No separate bridge needed.

---

## Instruction File Layering Strategy

All platforms support a layered instruction architecture. Layer from broadest to narrowest:

```
Layer 1: AGENTS.md          (universal — all platforms read this)
Layer 2: {PLATFORM}.md      (platform-specific extensions)
Layer 3: applyTo / rules    (scoped to file patterns or concerns)
Layer 4: Skills             (on-demand, loaded only when triggered)
```

### Token Impact by Layer

| Layer | When Loaded | Token Cost | Optimization |
|-------|-------------|------------|--------------|
| AGENTS.md | Every session | ~4K tokens | Keep under 15 KB |
| CLAUDE/CODEX/GEMINI.md | Every session | ~2K tokens | Keep under 8 KB |
| Scoped instructions | Per-file match | ~1K tokens | One concern per block |
| Skills | On-demand only | ~2.5K tokens | Lazy-load references |

### Deduplication Rules

| Content | Lives In | Referenced By |
|---------|----------|---------------|
| Project identity & architecture | AGENTS.md | All platform files |
| Platform-specific reasoning style | {PLATFORM}.md | Only that platform |
| File-pattern-specific rules | `applyTo` blocks | Matched files only |
| Reusable methodology | `.github/skills/` | On-demand |

**Never duplicate** AGENTS.md content in CLAUDE.md — use "Read AGENTS.md first" directive.

---

## Token Budget Allocation by Platform

Target token usage for project instruction files:

| Platform | Context Size | Instruction Budget | Skills Budget | Remaining for Code |
|----------|-------------|-------------------|---------------|-------------------|
| Copilot CLI | ~200K | 8K (4%) | 3K on-demand | 189K |
| Claude Code | ~200K | 10K (5%) | 3K on-demand | 187K |
| Gemini CLI | ~1M | 15K (1.5%) | 5K (rules loaded) | 980K |
| Codex CLI | ~128K | 6K (4.7%) | 3K on-demand | 119K |

**Codex is the constraint.** If instructions fit Codex's budget, they fit everywhere.

---

## Checklist: Making a Skill Portable

When creating a new skill, ensure cross-platform portability:

| Step | Action | Platforms |
|------|--------|-----------|
| 1 | Create `.github/skills/{cat}/{name}/SKILL.md` | All (universal) |
| 2 | Create `references/*.md` for deep-dives | All (universal) |
| 3 | Add entry to `.github/skills/CATALOG.md` | All (discovery) |
| 4 | Create `.claude/skills/{name}/SKILL.md` bridge | Claude Code |
| 5 | Create `.agent/rules/{name}.md` condensed rules | Gemini CLI |
| 6 | Verify AGENTS.md Skills Catalog section is current | Codex CLI |
| 7 | Set `metadata.platforms` in SKILL.md frontmatter | All (metadata) |
