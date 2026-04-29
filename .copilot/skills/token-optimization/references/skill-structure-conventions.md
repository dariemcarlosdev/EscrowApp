# Skill Structure Conventions — Token-Optimized

> Reference for: `.github/skills/workflow/token-optimization/SKILL.md`
> Load when: Creating or refactoring skills in `.github/skills/`.

---

## Universal Skill Anatomy

Every skill follows a three-layer structure optimized for progressive disclosure:

```
.github/skills/{category}/{skill-name}/
├── SKILL.md                    ← Core file (6–10 KB). Agent reads THIS first.
└── references/
    ├── topic-a.md              ← Deep-dive (3–5 KB). Loaded ONLY when needed.
    ├── topic-b.md
    └── topic-c.md
```

**Token math:**
- Agent always loads: SKILL.md (~2.5K tokens)
- Agent sometimes loads: 1 reference (~1.2K tokens)
- Agent rarely loads: 2+ references
- Effective average: ~3K tokens per skill invocation vs ~8K if everything were in one file

---

## SKILL.md Template

```yaml
---
name: skill-name
description: "One sentence — what this skill does"
license: MIT
allowed-tools: Read, Grep, Glob, Bash, Write
metadata:
  version: "1.0.0"
  domain: category-name
  triggers: comma, separated, trigger, phrases
  role: what-the-agent-becomes
  scope: what-it-covers
  platforms: copilot-cli, claude, gemini, codex
  output-format: what-it-produces
  related-skills: skill-a, skill-b
---

# Skill Title

You are a **[Role]** that [one sentence about what the agent does when this skill is active].

## When to Use This Skill

- Trigger condition 1
- Trigger condition 2
- Trigger condition 3

---

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| Topic A | `references/topic-a.md` | Condition for loading |
| Topic B | `references/topic-b.md` | Condition for loading |

---

## Core Workflow

### Step 1 — [Action]

[Instructions]

✅ **Checkpoint:** [What must be true before proceeding]

### Step 2 — [Action]

[Instructions]

✅ **Checkpoint:** [What must be true before proceeding]

[... repeat for each step ...]

---

## Constraints

### MUST DO
- [Non-negotiable requirement]

### MUST NOT
- [Anti-pattern to avoid]

---

## Anti-Patterns

| Anti-Pattern | Why It's Bad | Fix |
|---|---|---|
| Pattern X | Wastes Y | Do Z instead |
```

---

## Reference File Template

```markdown
# Topic Title — [Parent Skill Name]

> Reference for: `.github/skills/{category}/{skill-name}/SKILL.md`
> Load when: [Specific condition from the Reference Guide table].

---

## [Section 1]

[Content — tables preferred]

## [Section 2]

[Content — keep under 5 KB total]
```

---

## Key Conventions

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Skill directory | kebab-case | `token-optimization/` |
| Core file | Always `SKILL.md` | `SKILL.md` |
| References | kebab-case `.md` | `doc-authoring-patterns.md` |
| Category dir | kebab-case | `workflow/`, `security/`, `code-quality/` |

### YAML Frontmatter Rules

| Field | Required | Purpose |
|-------|----------|---------|
| `name` | ✅ | Skill identifier (kebab-case) |
| `description` | ✅ | One-sentence purpose |
| `metadata.version` | ✅ | Semver for change tracking |
| `metadata.domain` | ✅ | Category name |
| `metadata.triggers` | ✅ | Comma-separated trigger phrases for discovery |
| `metadata.platforms` | ✅ | Which AI platforms support this skill |
| `metadata.related-skills` | Recommended | Skills that complement this one |
| `metadata.role` | Recommended | What the agent becomes |
| `metadata.scope` | Recommended | Boundaries of the skill |

### Progressive Disclosure Rules

1. **SKILL.md gives the workflow.** Agent follows steps 1–N to complete the task.
2. **References give the depth.** Agent loads a reference ONLY when a specific step needs it.
3. **Reference Guide table is the contract.** It tells the agent exactly which reference to load and when.
4. **Never load all references at once.** Each reference is loaded only when the agent reaches the relevant step.

### Size Budgets

| Component | Target | Hard Max | Action if Over |
|-----------|--------|----------|----------------|
| SKILL.md | 6–8 KB | 10 KB | Move tables/examples to references |
| Reference | 3–4 KB | 5 KB | Split into two references |
| Total skill | 15–20 KB | 30 KB | Re-evaluate if skill scope is too broad |
| References count | 2–4 | 6 | Consider splitting into two skills |

---

## CATALOG.md Entry Format

When adding a skill to `.github/skills/CATALOG.md`:

```markdown
| `{category}/{skill-name}` | One-line description | trigger1, trigger2 | Refs: N |
```

Update the header version and skill count:
```markdown
> Version: X.Y.Z | Skills: NN | Categories: NN
```

---

## Bridge File Patterns

Each platform gets a lightweight bridge pointing to the universal skill:

### Claude (`.claude/skills/{name}/SKILL.md`)

```markdown
---
name: skill-name
description: "Same as universal"
---

# Skill Name — Claude Code Bridge

> This is a bridge file. Full skill: `.github/skills/{category}/{skill-name}/SKILL.md`

## How to Use

Read and follow the full skill workflow:
\```
cat .github/skills/{category}/{skill-name}/SKILL.md
\```

Then load references on-demand:
\```
cat .github/skills/{category}/{skill-name}/references/{topic}.md
\```
```

### Gemini (`.agent/rules/{skill-name}.md`)

```markdown
# Skill Name — Rules

> Full workflow: `.github/skills/{category}/{skill-name}/SKILL.md`

[Condensed rules — 20-30 lines max. The declarative subset of the skill.]
```

### Codex

Codex reads `AGENTS.md` which references `.github/skills/CATALOG.md`. No separate bridge needed — Codex discovers skills via the catalog path documented in AGENTS.md.

### Copilot CLI

Copilot CLI reads `.github/skills/` directly when skills are registered in the project. No bridge file needed.
