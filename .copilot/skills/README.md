# Universal AI Skills

Reusable methodology files that **any** AI assistant (Copilot CLI, Claude, Gemini) can load and follow step-by-step.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Type** | Markdown files — NOT tool invocations |
| **Loading** | Read with file tools (`view`, `cat`, `Read`) |
| **Architecture** | `SKILL.md` (core workflow, ~5KB) + `references/` (deep-dive topics) |
| **Disclosure** | Progressive — load only the reference needed for the current sub-task |
| **Naming** | Kebab-case skill names; category folders match concern areas |
| **Count** | 12 categories, 43 skills |

## How to Use a Skill

```bash
# 1. Find the right skill
cat .github/skills/CATALOG.md

# 2. Read the core workflow
cat .github/skills/{category}/{skill-name}/SKILL.md

# 3. Follow the numbered Core Workflow steps (each has a ✅ checkpoint)

# 4. Load references on-demand per the Reference Guide table
cat .github/skills/{category}/{skill-name}/references/{topic}.md
```

> **Never load all references at once.** Use the Reference Guide table in SKILL.md to pick only what your current sub-task needs.

## Skill Categories

| Category | Skills | Focus |
|----------|--------|-------|
| `ai/` | agent-orchestrator, mcp-developer, prompt-engineer | AI tooling and prompt design |
| `architecture/` | architecture-reviewer, dependency-analyzer, design-pattern-advisor, legacy-modernizer | System design and patterns |
| `code-quality/` | code-documenter, code-reviewer, debugging-wizard, quality-analyzer, refactor-planner, smart-refactor, tech-debt-tracker | Code health and maintenance |
| `database/` | query-optimizer, schema-reviewer | Data layer quality |
| `devops/` | chaos-engineer, ci-cd-builder, deployment-preflight, monitoring-expert | Operations and CI/CD |
| `documentation/` | adr-creator, api-documenter, readme-generator | Documentation generation |
| `language/` | csharp-developer, dotnet-core-expert | Language-specific expertise |
| `project-management/` | feature-forge, issue-creator, mvp-gatekeeper, spec-writer | Planning and specifications |
| `research/` | codebase-explorer, deep-context-generator, spec-miner, tech-spike-planner | Investigation and discovery |
| `security/` | authentication, authorization, owasp-audit, secret-scanner, threat-modeler | Security analysis |
| `testing/` | tdd-coach, test-coverage-analyzer, test-generator | Test strategy and generation |
| `workflow/` | memory-optimization | Context and token optimization |

## Skill File Structure

```
skills/
├── CATALOG.md                          ← Master index of all skills
└── {category}/
    └── {skill-name}/
        ├── SKILL.md                    ← Core workflow (~5KB) with checkpoints
        └── references/
            ├── topic-a.md              ← Deep-dive loaded on demand
            └── topic-b.md
```

## Creating a New Skill

1. Create a folder: `.github/skills/{category}/{skill-name}/`
2. Write `SKILL.md` with:
   - **Core Workflow** — numbered steps with ✅ checkpoints
   - **Reference Guide table** — maps sub-tasks to reference files with "load when" guidance
3. Add `references/` folder with deep-dive markdown files as needed.
4. Register the skill in `CATALOG.md`.

## Key Rules

- **Read, don't invoke** — skills are files, not tools. Use file-reading commands.
- **One skill at a time** — only read the skill matching the current task.
- **Progressive disclosure** — never load all references; follow the Reference Guide table.
- **Follow checkpoints** — each Core Workflow step has a ✅; verify before proceeding.

## See Also

- [`CATALOG.md`](CATALOG.md) — Full catalog with all 43 skills and descriptions
- [`.github/instructions/`](../instructions/) — Pattern-matched instructions (auto-injected, not on-demand)
- [`.github/extensions/superpowers/`](../extensions/superpowers/) — Extension that exposes `superpowers_catalog` and `superpowers_skill` tools for convenient skill loading
