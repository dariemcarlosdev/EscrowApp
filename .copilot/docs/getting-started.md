# Getting Started with NexTruzt.io AI Skills

> Universal guide for setting up the 50-skill AI development infrastructure.

## Prerequisites

- Git repository cloned locally
- One of: Copilot CLI, Claude Code, Codex CLI, or Gemini (via Cursor/Windsurf/OpenCode)

## Quick Start (Any Tool)

### 1. Verify Skills Infrastructure

```bash
# Check the catalog
cat .github/skills/CATALOG.md

# Check the meta-skill (discovery guide)
cat .github/skills/workflow/using-skills/SKILL.md
```

### 2. Use a Skill

```bash
# Read the skill you need
cat .github/skills/{category}/{skill-name}/SKILL.md

# Follow the Core Workflow steps
# Load references on-demand as directed by the Reference Guide table
```

### 3. Use the Right Tool

| AI Tool | Instructions File | Skill Access | Commands |
|---|---|---|---|
| **Copilot CLI** | `.github/copilot-instructions.md` | `.github/skills/` directly | N/A |
| **Claude Code** | `CLAUDE.md` + `.claude/skills/` | Via bridge files → `.github/skills/` | `.claude/commands/` (`/spec`, `/build`, etc.) |
| **Codex CLI** | `CODEX.md` + `AGENTS.md` | `.github/skills/` directly | N/A |
| **Gemini** | `GEMINI.md` + `.agent/rules/` | Rules loaded eagerly | N/A |

---

## Architecture Overview

```
.github/skills/              ← Universal source of truth (50 skills)
├── CATALOG.md                ← Master index (v3.0.0)
├── {category}/               ← 12 categories
│   └── {skill-name}/
│       ├── SKILL.md          ← Core workflow (6-10 KB)
│       └── references/       ← Deep-dive docs (load on demand)

.claude/                      ← Claude Code integration
├── skills/{name}/SKILL.md    ← Bridge files → .github/skills/
└── commands/{name}.md        ← Slash commands (10 commands)

.claude-plugin/               ← Claude marketplace config
├── plugin.json
└── marketplace.json

.agent/rules/                 ← Gemini rules (eagerly loaded)
└── {name}.md                 ← Condensed declarative format

.github/agents/               ← Agent personas (3 roles)
├── code-reviewer.md
├── test-engineer.md
└── security-auditor.md

.github/hooks/                ← Session lifecycle hooks
├── hooks.json
├── session-start.ps1
└── session-start.sh
```

---

## Skill Categories (12)

| Category | Skills | Focus |
|---|---|---|
| `code-quality` | 8 | Code review, refactoring, debugging, quality metrics |
| `security` | 5 | OWASP audit, secrets, threats, auth |
| `architecture` | 5 | Architecture review, patterns, dependencies |
| `testing` | 3 | Test generation, TDD, coverage analysis |
| `database` | 2 | Schema review, query optimization |
| `devops` | 5 | CI/CD, deployment, monitoring, chaos, git workflow |
| `documentation` | 3 | README, ADR, API docs |
| `research` | 4 | Codebase explorer, tech spikes, source-driven dev |
| `project-management` | 5 | Spec writing, feature forge, MVP gatekeeper, idea refine |
| `ai` | 3 | MCP development, prompt engineering, multi-agent planning |
| `language` | 2 | .NET Core, C# expertise |
| `workflow` | 3 | Memory optimization, token optimization, using-skills (meta) |

---

## Token Efficiency

This infrastructure is designed for minimal token consumption:

1. **Progressive disclosure** — Skills have a thin core (6-10 KB) with deeper references loaded on demand
2. **Bridge files** — Claude bridges are ~30 lines, not full skill copies
3. **Gemini rules** — Condensed to ~40 lines of declarative rules
4. **Catalog as index** — One file to find any skill without loading all of them
5. **Reference Guide tables** — Tell you WHEN to load each reference, not "load everything"

---

## Adding New Skills

See the token-optimization skill for best practices:

```bash
cat .github/skills/workflow/token-optimization/SKILL.md
```

Every new skill needs:
1. Universal SKILL.md in `.github/skills/{category}/{skill-name}/`
2. Claude bridge in `.claude/skills/{name}/SKILL.md`
3. Gemini rule in `.agent/rules/{name}.md`
4. Entry in `.github/skills/CATALOG.md`
5. Count updates in AGENTS.md, CLAUDE.md, GEMINI.md, copilot-instructions.md
