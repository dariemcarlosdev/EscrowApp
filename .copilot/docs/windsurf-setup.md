# Windsurf Setup — NexTruzt.io AI Skills

> Configure Windsurf (Codeium) to use the 50-skill AI development infrastructure.

## Setup Steps

### 1. Cascade Rules

Windsurf uses `.windsurfrules` or `.windsurf/rules/` for project instructions.

Create `.windsurfrules` in the project root:

```markdown
# NexTruzt.io EscrowApp — Windsurf Rules

Read AGENTS.md for full project context.

## Skills

50 AI skills in `.github/skills/`. Read the catalog:
cat .github/skills/CATALOG.md

Before implementing, check the meta-skill:
cat .github/skills/workflow/using-skills/SKILL.md

## Code Conventions

- Blazor code-behind: .razor + .razor.cs + .razor.css
- MediatR for all business operations
- File-scoped namespaces, sealed classes, record DTOs
- [Authorize] on every endpoint
- IStringLocalizer for all user-facing strings
```

### 2. Using Skills in Windsurf

Windsurf's Cascade can read files. Use the same pattern:

```
1. Find skill: cat .github/skills/CATALOG.md
2. Read skill: cat .github/skills/{category}/{skill-name}/SKILL.md
3. Follow Core Workflow
4. Load references on-demand
```

### 3. Agent Personas

When configuring Windsurf agents, reference:

- `.github/agents/code-reviewer.md` for review tasks
- `.github/agents/test-engineer.md` for test generation
- `.github/agents/security-auditor.md` for security audits

### 4. Gemini Rules

If Windsurf is configured with Gemini, it may load `.agent/rules/` automatically.
These contain condensed skill summaries optimized for eager-loading models.
