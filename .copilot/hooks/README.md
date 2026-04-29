# Claude Code Hooks

PowerShell scripts that run automatically on Claude Code events (PreToolUse, PostToolUse, Notification, SessionStart). They enforce project standards, remind about best practices, and automate quality checks — all without manual intervention.

## At a Glance

| Aspect | Detail |
|--------|--------|
| **Runtime** | PowerShell, executed locally by Claude Code |
| **Trigger** | Registered events in `.claude/settings.json` (`hooks` key) |
| **Constraint** | Must complete in < 2 seconds to avoid blocking the AI agent |
| **Relationship** | Complements `.github/` CI checks — hooks run locally and instantly; CI runs remotely on push |

## Event Types

| Event | When | Can Block? |
|-------|------|------------|
| `PreToolUse` | Before a tool executes (edit, create, bash, etc.) | Yes — non-zero exit prevents the action |
| `PostToolUse` | After a tool completes | No — advisory only |
| `Notification` | Custom triggers from other hooks or config | No |
| `SessionStart` | When a Claude Code session begins | No |

## Current Hooks

| Script | Purpose |
|--------|---------|
| `build-reminder.ps1` | Reminds to verify builds after code changes |
| `context-optimizer.ps1` | Suggests efficient context-loading strategies |
| `doc-sync-reminder.ps1` | Reminds to update `docs/` when source code changes |
| `dotnet-conventions.ps1` | Checks .NET coding conventions on edited files |
| `research-first.ps1` | Prompts for codebase research before making changes |
| `security-scanner.ps1` | Runs OWASP security checks on modified files |
| `test-runner.ps1` | Reminds to run tests after implementation changes |

## Notification System

| File | Role |
|------|------|
| `notification-config.json` | Defines notification rules (severity, targets, cooldowns) |
| `notification.ps1` | Centralized delivery engine — reads config, formats output |
| `notifications.log` | Append-only history of delivered notifications |
| `.rate-limit-timestamp` | Prevents reminder spam by tracking last trigger time |

## Creating a New Hook

1. Write a `.ps1` script in this directory.
2. Register it in `.claude/settings.json` (or `settings.local.json`) under the `hooks` key for the appropriate event.
3. Keep execution under 2 seconds — offload heavy work to background jobs if needed.
4. Use rate limiting (`[datetime]::UtcNow` checks) to prevent reminder fatigue.
5. **Never** log PII, tokens, or secrets in hook output.

## See Also

- `.claude/rules/` — Always-on behavioral rules for Claude Code
- `.claude/skills/` — Skill bridges for Claude Code's `/skills` system
- `.github/instructions/` — Copilot instruction files (pattern-triggered, different activation model)
