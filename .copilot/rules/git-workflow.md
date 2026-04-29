# Git Workflow Rules — Gemini Agent
# Source: .github/skills/devops/git-workflow/SKILL.md

## When Active
- Creating branches, commits, PRs, or managing version control
- Any git operation during development

## Branch Naming
- `feature/{short-description}` — New features
- `fix/{short-description}` — Bug fixes
- `chore/{short-description}` — Maintenance, dependencies
- `docs/{short-description}` — Documentation only

## Commit Conventions (Conventional Commits)
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation only
- `refactor:` Code restructure, no behavior change
- `test:` Adding/updating tests
- `chore:` Build, CI, tooling

## Rules
1. **Atomic commits** — One logical change per commit
2. **Descriptive messages** — Future you searches git log. Messages ARE documentation.
3. **Never force-push** to shared branches
4. **PR description** — Include: what changed, why, acceptance criteria, linked issue
5. **Co-authored-by trailer** — Always include for AI-assisted commits:
   `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`

## Anti-Rationalization
- "I'll clean up later" → You won't. Commit hygiene degrades over time.
- "Message doesn't matter" → It does. Search your git log in 6 months.
- "I'll squash everything" → Atomic commits enable bisect and safe revert.
