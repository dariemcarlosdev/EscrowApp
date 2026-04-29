# Using Skills (Meta-Skill) — Gemini Agent
# Source: .github/skills/workflow/using-skills/SKILL.md

## When Active
- ALWAYS — this meta-skill governs how all other skills are discovered and used
- At session start, check for applicable skills before starting work

## Skill Discovery
- Task is vague idea → idea-refine
- Need a spec → spec-writer
- Need task breakdown → feature-forge
- Implementing code → incremental-implementation
- Writing tests → test-generator / tdd-coach
- Something broke → debugging-wizard
- Code review → code-reviewer
- Security concern → owasp-audit
- Refactoring → smart-refactor
- CI/CD → ci-cd-builder
- Documentation → adr-creator / readme-generator
- Planning AI features → multi-agent-planner
- Creating skills/docs → token-optimization
- Git/versioning → git-workflow
- Deploying → deployment-preflight

## Core Operating Behaviors (apply to ALL skills)
1. **Surface Assumptions** — State assumptions before implementing
2. **Manage Confusion** — Stop and ask when ambiguous
3. **Push Back** — Challenge bad approaches honestly
4. **Enforce Simplicity** — Resist over-engineering
5. **Maintain Scope** — Touch only what was asked
6. **Verify, Don't Assume** — Evidence over "seems right"

## Rules
- Check for applicable skill BEFORE starting work
- Skills are workflows — follow steps in order
- Multiple skills can chain (e.g., spec → build → test → review)
- Full catalog: `.github/skills/CATALOG.md`

## Feature Lifecycle Sequence
idea-refine → spec-writer → feature-forge → source-driven-development → incremental-implementation → tdd-coach → code-reviewer → git-workflow → deployment-preflight
