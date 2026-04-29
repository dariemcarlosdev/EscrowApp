# OpenCode Setup — NexTruzt.io AI Skills

> Configure OpenCode CLI to use the 50-skill AI development infrastructure.

## About OpenCode

OpenCode is an open-source terminal-based AI coding assistant. It reads project context from
standard files and supports multiple LLM providers.

## Setup Steps

### 1. Project Context

OpenCode reads `AGENTS.md` automatically if present in the project root. Our AGENTS.md contains:
- Full architecture overview
- CQRS/MediatR patterns
- Blazor component rules
- Security requirements
- Skills catalog reference

No additional configuration needed for basic operation.

### 2. Using Skills

Skills are file-based — any tool that can read files can use them:

```bash
# Find the right skill
cat .github/skills/CATALOG.md

# Read and follow a skill
cat .github/skills/{category}/{skill-name}/SKILL.md

# Load references on-demand
cat .github/skills/{category}/{skill-name}/references/{topic}.md
```

### 3. Agent Personas

Reference persona files when you need role-specific behavior:

```bash
# For code review
cat .github/agents/code-reviewer.md

# For test writing
cat .github/agents/test-engineer.md

# For security audit
cat .github/agents/security-auditor.md
```

### 4. Session Hooks

Run the session start hook manually to prime context:

```bash
# Unix/Mac
bash .github/hooks/session-start.sh

# Windows
powershell -File .github/hooks/session-start.ps1
```

### 5. Skill Discovery Flow

```
Task arrives → Read using-skills meta-skill → Follow discovery flowchart → Use the right skill
```

Full meta-skill: `cat .github/skills/workflow/using-skills/SKILL.md`
