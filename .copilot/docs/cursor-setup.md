# Cursor Setup — NexTruzt.io AI Skills

> Configure Cursor IDE to use the 50-skill AI development infrastructure.

## Setup Steps

### 1. Project Rules (Automatic)

Cursor reads `.cursor/rules/` for project-specific instructions. Create rules that reference our skills:

```bash
mkdir -p .cursor/rules
```

Create `.cursor/rules/escrowapp.md`:

```markdown
# NexTruzt.io EscrowApp — Cursor Rules

Read AGENTS.md for full project context.

## Skills Available

50 AI skills in `.github/skills/`. Read CATALOG.md for the full index:
- `cat .github/skills/CATALOG.md`

## Before Implementing

1. Check for an applicable skill: `cat .github/skills/workflow/using-skills/SKILL.md`
2. Follow the skill's Core Workflow
3. Load references on-demand from the skill's Reference Guide table

## Code Conventions

- Code-behind pattern for Blazor (3 files per component)
- MediatR for all business operations
- File-scoped namespaces, sealed classes, record DTOs
- IStringLocalizer for all user-facing strings
```

### 2. Agent Personas

Reference the agent persona files in your Cursor agent configuration:

| Persona | File | Use When |
|---|---|---|
| Code Reviewer | `.github/agents/code-reviewer.md` | Reviewing PRs or code changes |
| Test Engineer | `.github/agents/test-engineer.md` | Writing or reviewing tests |
| Security Auditor | `.github/agents/security-auditor.md` | Security reviews |

### 3. Gemini Model in Cursor

If using Gemini as the model in Cursor, it will automatically load `.agent/rules/` files.
These contain condensed versions of our skills optimized for Gemini's eager-loading behavior.

### 4. Quick Reference

```bash
# Find the right skill
cat .github/skills/CATALOG.md

# Read a skill
cat .github/skills/{category}/{skill-name}/SKILL.md

# Load a deep-dive reference
cat .github/skills/{category}/{skill-name}/references/{topic}.md
```
