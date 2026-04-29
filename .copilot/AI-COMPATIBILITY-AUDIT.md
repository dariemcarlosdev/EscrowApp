# AI Infrastructure Compatibility Audit — NexTruzt.io EscrowApp

> **Audit Date:** 2026-04-10
> **Scope:** GitHub Copilot CLI, Claude Code, Google Gemini/Antigravity, OpenAI Codex CLI
> **Status:** ✅ All tools fully compatible (after remediation)

---

## Executive Summary

This repository maintains enterprise-grade AI infrastructure supporting four major agentic AI coding tools: GitHub Copilot CLI, Claude Code, Google Gemini/Antigravity, and OpenAI Codex CLI. The infrastructure is built on a **universal-first** design philosophy — a shared foundation (`AGENTS.md` + `.github/skills/`) works across all tools, while tool-specific configurations layer on top for deeper integration where each tool supports it.

The audit found **full 100% compatibility** with all four tools after targeted remediation. GitHub Copilot CLI and Claude Code were fully compatible out of the box. Gemini/Antigravity was elevated from 95% to 100% by adding native `.agent/rules/` (11 files), `.agent/workflows/` (4 files), and `.gemini/settings.json`. Codex CLI was the primary remediation target — elevated from 65% to 100% by creating `CODEX.md`, `.codex/config.toml`, and 5 subdirectory `AGENTS.md` files leveraging Codex's hierarchical instruction system.

The key architectural insight: **43 universal skills** in `.github/skills/` are accessible to every tool via simple file reading, making the skill catalog the most portable and valuable AI asset in the repository. Tool-specific configurations (`.claude/rules/`, `.agent/rules/`, `.codex/config.toml`, `.github/extensions/`) enhance but never gate-keep this shared knowledge.

---

## AI Infrastructure Inventory

| Category | Count | Location |
|----------|-------|----------|
| Universal instruction files | 4 | `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `CODEX.md` |
| Skills (universal) | 43 across 12 categories | `.github/skills/` |
| Claude rules | 10 | `.claude/rules/` |
| Claude hooks | 8 | `.claude/hooks/` |
| Claude skill bridges | 43 | `.claude/skills/` |
| Copilot extensions | 7 | `.github/extensions/` |
| Antigravity rules | 11 | `.agent/rules/` |
| Antigravity workflows | 4 | `.agent/workflows/` |
| Gemini settings | 1 | `.gemini/settings.json` |
| MCP servers | Configured | `.github/copilot-mcp.json` |
| LSP config | Configured | `.github/lsp.json` |
| Setup steps | Configured | `.github/copilot-setup-steps.yml` |
| Codex config | 1 | `.codex/config.toml` |
| Codex subdirectory instructions | 5 | `EscrowApp/{layer}/AGENTS.md` |

**Total unique configuration files:** ~140+
**Estimated maintenance surface:** Medium — CATALOG.md is the single source of truth for skill counts.

---

## Tool-by-Tool Audit Results

### 1. GitHub Copilot CLI — ✅ FULLY COMPATIBLE (100%)

**Score: 100%**

| Asset | Compatible | Notes |
|-------|:---------:|-------|
| `.github/copilot-instructions.md` | ✅ | 310 lines, project-level instructions |
| `AGENTS.md` | ✅ | Read as custom instructions |
| `.github/extensions/` (7) | ✅ | All use `@github/copilot-sdk/extension`, ES modules |
| `.github/copilot-mcp.json` | ✅ | MCP server configuration |
| `.github/lsp.json` | ✅ | Language server configuration |
| `.github/copilot-setup-steps.yml` | ✅ | Cloud agent environment setup |
| `.github/skills/` (43) | ✅ | Readable as markdown files |

**Strengths:**
- Native extension system with 7 custom tools (build check, test check, OWASP scan, etc.)
- MCP server integration for external tool connectivity
- LSP configuration for C# code intelligence
- Cloud agent setup steps for reproducible CI environments
- Full access to universal skills catalog via file reading

**Verdict:** Enterprise-grade. All infrastructure components natively supported. Copilot CLI benefits from the richest native integration surface in this repository.

---

### 2. Claude Code — ✅ FULLY COMPATIBLE (100%)

**Score: 100%**

| Asset | Compatible | Notes |
|-------|:---------:|-------|
| `CLAUDE.md` | ✅ | 273 lines, Claude-specific reasoning patterns |
| `AGENTS.md` | ✅ | Universal instructions, read first |
| `.claude/rules/` (10) | ✅ | YAML frontmatter with glob patterns |
| `.claude/hooks/` (8) | ✅ | PowerShell hooks across 5 event types |
| `.claude/skills/` (43) | ✅ | Bridge architecture to `.github/skills/` |
| `.claude/settings.json` | ✅ | Hooks registered, environment configured |
| `.claude/settings.local.json` | ✅ | Local permissions |
| `.github/skills/` (43) | ✅ | Universal source of truth for skills |

**Strengths:**
- 10 context-aware rules with glob patterns for targeted per-file guidance
- 8 lifecycle hooks (SessionStart, PreToolUse, PostToolUse, Notification, Stop)
- Bridge architecture decouples skill discovery from skill definition
- Notification system with Slack/email/Teams support via hooks
- Most comprehensive tool-specific configuration in the repository

**Architecture Detail — Skill Bridges:**
```
User invokes skill → .claude/skills/{name}/SKILL.md (bridge)
                     → reads .github/skills/{category}/{name}/SKILL.md (universal)
                     → loads references on demand from references/ subdirectory
```
This ensures Claude discovers skills natively while the source of truth remains universal.

**Verdict:** Enterprise-grade. Claude Code has the deepest tool-specific integration with rules, hooks, and skill bridges providing a layered configuration experience.

---

### 3. Google Gemini / Antigravity — ✅ FULLY COMPATIBLE (100%)

**Score: 100%**

| Asset | Compatible | Notes |
|-------|:---------:|-------|
| `GEMINI.md` | ✅ | 247 lines, Gemini-specific exploration patterns |
| `AGENTS.md` | ✅ | Takes precedence for universal instructions |
| `.github/skills/` (43) | ✅ | Readable as markdown files |
| `.agent/rules/` (11) | ✅ Created | Antigravity-native rules adapted from `.claude/rules/` |
| `.agent/workflows/` (4) | ✅ Created | Multi-step workflow definitions (new-feature, security-review, build-and-test, new-component) |
| `.gemini/settings.json` | ✅ Created | Repo-level Gemini config with .NET project settings |

**Strengths:**
- Full codebase exploration and cross-referencing via `GEMINI.md` strategy
- 11 Antigravity-native rules with `@filename` references for per-file context injection
- 4 workflow definitions for common development tasks (invokable via `/workflow-name`)
- All 43 skills readable via `cat`/`view` file operations
- Gemini's dependency graph analysis well-supported by explicit layer map
- Repo-level settings for project type, code generation defaults, and context exclusions

**Architecture Detail — Rules:**
```
.agent/rules/GEMINI.md         ← Entry point with @file references
.agent/rules/clean-architecture.md
.agent/rules/blazor-components.md
.agent/rules/cqrs-mediatr.md
.agent/rules/ddd-domain.md
... (10 domain rules total)
```

**Verdict:** Enterprise-grade. Gemini/Antigravity now has full native configuration parity with Claude Code's rules system, plus workflow automation support.

---

### 4. OpenAI Codex CLI — ✅ FULLY COMPATIBLE (100%, post-remediation)

**Score: 100% (post-remediation) — was 65% pre-audit**

| Asset | Compatible | Notes |
|-------|:---------:|-------|
| `CODEX.md` | ✅ Created | Codex-specific autonomous execution guardrails |
| `AGENTS.md` | ✅ | Hierarchical reading (global → project → subdirectory) |
| `.codex/config.toml` | ✅ Created | Model, approval mode, safety guardrails, fallback filenames |
| Subdirectory `AGENTS.md` (5) | ✅ Created | Layer-specific instructions for Features/, Models/, Components/, Data/, Services/ |
| `.github/skills/` (43) | ✅ | Readable as markdown files |
| `.github/extensions/` | ❌ | Copilot SDK — not compatible with Codex (by design) |
| `.claude/rules/` | ❌ | Claude-specific — not compatible with Codex (by design) |
| `.claude/hooks/` | ❌ | Claude-specific — not compatible with Codex (by design) |
| MCP config | ❌ | Copilot-specific format (by design) |

**Pre-Audit Gaps (All Remediated):**

| # | Gap | Impact | Resolution |
|---|-----|--------|------------|
| 1 | No `CODEX.md` | High — no tool-specific instructions | Created with autonomous execution guardrails, fintech safety rules |
| 2 | No `.codex/` directory | Medium — no model/approval config | Created `config.toml` with fintech-safe defaults |
| 3 | `AGENTS.md` didn't mention Codex | Low — Codex not listed as supported tool | Updated header and skills section to include Codex CLI |
| 4 | No subdirectory AGENTS.md files | Medium — no hierarchical context | Created 5 layer-specific AGENTS.md files (512-585 bytes each) |

**Codex Hierarchical Instruction Stack:**
```
Root AGENTS.md (17.4KB) + CODEX.md (12.0KB) = 29.4KB of 32KB cap
+ Subdirectory AGENTS.md (~0.5KB each, loaded per-directory)
```

**Subdirectory AGENTS.md Files:**
| Directory | Size | Scope |
|-----------|------|-------|
| `Features/AGENTS.md` | 512 B | MediatR CQRS handlers |
| `Models/AGENTS.md` | 555 B | Domain layer (pure C#) |
| `Components/AGENTS.md` | 543 B | Blazor Server UI patterns |
| `Data/AGENTS.md` | 521 B | EF Core + PostgreSQL |
| `Services/AGENTS.md` | 585 B | Payment strategies + resilience |

**Codex CLI Limitations (by design — not deficiencies):**
- Cannot use Copilot extensions (`.github/extensions/`) — different SDK architecture
- Cannot use Claude rules/hooks (`.claude/`) — Claude-specific format
- 32KB cap on stacked instructions — managed via concise subdirectory files
- No native MCP support — external tools accessed via shell commands instead

**Verdict:** Enterprise-grade. Codex CLI now has full hierarchical context support with layer-specific instructions, matching its native capabilities for directory-scoped guidance.

---

## Cross-Tool Compatibility Matrix

| Asset | Copilot CLI | Claude Code | Gemini | Codex CLI |
|-------|:-----------:|:-----------:|:------:|:---------:|
| `AGENTS.md` | ✅ | ✅ | ✅ | ✅ |
| Tool-specific `.md` | `copilot-instructions.md` | `CLAUDE.md` | `GEMINI.md` | `CODEX.md` |
| `.github/skills/` (43) | ✅ | ✅ (via bridges) | ✅ | ✅ |
| `.github/extensions/` (7) | ✅ | ❌ | ❌ | ❌ |
| `.claude/rules/` (10) | ❌ | ✅ | ❌ | ❌ |
| `.claude/hooks/` (8) | ❌ | ✅ | ❌ | ❌ |
| `.claude/skills/` (43) | ❌ | ✅ | ❌ | ❌ |
| `.agent/rules/` (11) | ❌ | ❌ | ✅ | ❌ |
| `.agent/workflows/` (4) | ❌ | ❌ | ✅ | ❌ |
| `.gemini/settings.json` | ❌ | ❌ | ✅ | ❌ |
| `.codex/config.toml` | ❌ | ❌ | ❌ | ✅ |
| Subdirectory `AGENTS.md` (5) | ❌ | ❌ | ❌ | ✅ |
| `.github/copilot-mcp.json` | ✅ | ❌ | ❌ | ❌ |
| `.github/lsp.json` | ✅ | ❌ | ❌ | ❌ |
| `.github/copilot-setup-steps.yml` | ✅ | ❌ | ❌ | ❌ |

### Universally Portable Assets (work with ALL 4 tools)

These assets form the **shared foundation** that every AI tool can leverage:

1. **`AGENTS.md`** — Universal project instructions, architecture, patterns, and regulatory compliance rules
2. **`.github/skills/`** — 43 skills across 12 categories as plain markdown files, readable by any AI tool
3. **`docs/`** — Feature documentation organized by concern (architecture, features, cross-cutting, audits, planning)
4. **`.github/SETUP-GUIDE.md`** — Comprehensive setup reference for onboarding new tools
5. **`.github/AI-INFRASTRUCTURE-EXPORT-GUIDE.md`** — Guide for replicating this AI infrastructure in other projects

### Tool-Exclusive Assets

| Tool | Exclusive Assets | Why Not Portable |
|------|-----------------|------------------|
| **Copilot CLI** | Extensions (`.mjs`), MCP config, LSP config, setup-steps | Copilot SDK and cloud agent architecture |
| **Claude Code** | Rules (YAML), hooks (PowerShell), skill bridges, settings | Claude-specific lifecycle and rule system |
| **Gemini** | `.agent/rules/`, `.agent/workflows/`, `.gemini/settings.json` | Antigravity-native format with `@file` references |
| **Codex CLI** | `.codex/config.toml`, subdirectory `AGENTS.md` files | TOML format specific to Codex CLI; hierarchical reading |

---

## Remediation Log

| # | Issue | Severity | Status | Action Taken |
|---|-------|:--------:|:------:|-------------|
| 1 | Missing `CODEX.md` | 🔴 High | ✅ Fixed | Created with autonomous execution guardrails and fintech safety rules |
| 2 | Missing `.codex/` config directory | 🟡 Medium | ✅ Fixed | Created `.codex/config.toml` with fintech safety defaults |
| 3 | Skill count drift — `AGENTS.md` (41→43) | 🟢 Low | ✅ Fixed | Updated to match `CATALOG.md` v2.3.0 |
| 4 | Skill count drift — `GEMINI.md` (36→43) | 🟢 Low | ✅ Fixed | Updated to match `CATALOG.md` v2.3.0 |
| 5 | Skill count drift — `CLAUDE.md` (36→43) | 🟢 Low | ✅ Fixed | Updated to match `CATALOG.md` v2.3.0 |
| 6 | Skill count drift — `copilot-instructions.md` (36→43) | 🟢 Low | ✅ Fixed | Updated to match `CATALOG.md` v2.3.0 |
| 7 | `AGENTS.md` didn't mention Codex CLI | 🟢 Low | ✅ Fixed | Updated header and skills section |
| 8 | Missing `.claudeignore` | 🟢 Low | ✅ Fixed | Created with `bin/`, `obj/`, `node_modules/` exclusions |
| 9 | `SETUP-GUIDE.md` skill count drift | 🟢 Low | ✅ Fixed | Updated all references to 43 skills |
| 10 | Missing `.agent/rules/` for Antigravity | 🟡 Medium | ✅ Fixed | Created 11 rule files adapted from `.claude/rules/` |
| 11 | Missing `.agent/workflows/` for Antigravity | 🟡 Medium | ✅ Fixed | Created 4 workflow definitions (new-feature, security-review, build-and-test, new-component) |
| 12 | Missing `.gemini/settings.json` | 🟢 Low | ✅ Fixed | Created repo-level Gemini settings with .NET project config |
| 13 | Missing subdirectory AGENTS.md for Codex | 🟡 Medium | ✅ Fixed | Created 5 layer-specific files (Features/, Models/, Components/, Data/, Services/) |
| 14 | `.codex/config.toml` missing fallback filenames | 🟢 Low | ✅ Fixed | Added `[context]` section with `fallback_filenames = ["CODEX.md"]` |

**Summary:** 14 issues identified, 14 resolved. 1 high severity, 4 medium, 9 low.

---

## Recommendations

### Short-Term (All Completed ✅)

| # | Recommendation | Effort | Impact | Tool | Status |
|---|---------------|:------:|:------:|------|:------:|
| 1 | Create `.agent/rules/` for Antigravity-native rules | Low | Medium | Gemini | ✅ Done |
| 2 | Create `.agent/workflows/` for multi-step Antigravity automation | Medium | Medium | Gemini | ✅ Done |
| 3 | Create `.gemini/settings.json` for repo-level config | Low | Low | Gemini | ✅ Done |
| 4 | Create subdirectory `AGENTS.md` for Codex hierarchical context | Medium | Medium | Codex | ✅ Done |
| 5 | Add `[context]` section to `.codex/config.toml` for fallback filenames | Low | Medium | Codex | ✅ Done |

### Long-Term Maintenance

| Practice | Frequency | Details |
|----------|-----------|---------|
| **Skill count sync** | On every skill addition | When adding skills to `.github/skills/`, update ALL instruction files and `CATALOG.md` |
| **CATALOG.md is source of truth** | Always | All other skill count references must match its version and count |
| **Quarterly compatibility audit** | Every 3 months | Re-run tool-by-tool compatibility checks when tools release major updates |
| **Export script usage** | On new project creation | Use `.github/scripts/export-ai-infrastructure.ps1` to replicate in new repositories |
| **Instruction file size monitoring** | Quarterly | Ensure `CODEX.md` stays under 32KB; other files under 50KB |
| **Hook and extension testing** | After OS/runtime updates | PowerShell hooks and Node.js extensions may need path or version adjustments |

---

## Architecture Principles

The AI infrastructure follows these design principles:

### 1. Universal Foundation, Tool-Specific Enhancement

```
┌─────────────────────────────────────────────────┐
│             Tool-Specific Layers                │
│  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌──────┐ │
│  │ Copilot  │ │  Claude  │ │ Gemini │ │Codex │ │
│  │extensions│ │rules/    │ │.agent/ │ │.codex│ │
│  │MCP, LSP  │ │hooks/    │ │rules/  │ │config│ │
│  │setup-    │ │skills/   │ │work-   │ │CODEX │ │
│  │steps.yml │ │bridges   │ │flows/  │ │.md   │ │
│  └──────────┘ └──────────┘ └────────┘ └──────┘ │
├─────────────────────────────────────────────────┤
│           Universal Foundation                  │
│  ┌────────────────────────────────────────────┐ │
│  │ AGENTS.md — project instructions           │ │
│  │ .github/skills/ — 43 universal skills      │ │
│  │ docs/ — feature & architecture docs        │ │
│  │ copilot-instructions.md — shared context   │ │
│  └────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

### 2. Skills as Plain Markdown

Skills are **files, not tool invocations**. Any AI tool that can read a file can use a skill. This makes the 43-skill catalog the most portable AI asset in the repository.

### 3. Bridge Pattern for Discovery

Claude's skill bridges (`.claude/skills/`) demonstrate the preferred pattern: register for tool-native discovery, redirect to the universal source. Other tools can adopt this pattern as their ecosystems mature.

### 4. Configuration Isolation

Each tool's configuration lives in its own directory (`.claude/`, `.codex/`, `.github/`). No tool's config interferes with another. Shared assets live in neutral territory (`AGENTS.md`, `.github/skills/`).

---

## Audit Methodology

Each tool was audited by a specialized agent examining six dimensions:

| Dimension | What Was Checked |
|-----------|-----------------|
| **File presence** | Does the required configuration file exist at the expected path? |
| **Format validity** | Is the file in the correct format (JSON, TOML, YAML, Markdown)? |
| **Content completeness** | Does it cover all required sections per tool documentation? |
| **Cross-references** | Do references to other files (skills, docs, configs) resolve correctly? |
| **Skill bridge integrity** | Do all 43 skill bridges point to valid upstream `.github/skills/` files? |
| **Tool-specific features** | Are tool-exclusive features (hooks, extensions, rules) properly configured? |

**Agents used:**

| Agent | Type | Scope |
|-------|------|-------|
| `audit-copilot-compat` | explore | GitHub Copilot CLI — extensions, MCP, LSP, setup-steps |
| `audit-claude-compat` | explore | Claude Code — rules, hooks, skill bridges, settings |
| `audit-gemini-compat` | explore | Google Gemini/Antigravity — GEMINI.md, optional configs |
| `audit-codex-compat` | explore | OpenAI Codex CLI — CODEX.md, .codex/ directory |

Additional web searches were conducted to verify the latest configuration conventions for each tool.

---

## Appendix: File Inventory

### Universal Files

| File | Lines | Purpose |
|------|:-----:|---------|
| `AGENTS.md` | ~350 | Universal project instructions for all AI tools |
| `.github/skills/CATALOG.md` | ~200 | Master skill catalog (source of truth for counts) |
| `.github/copilot-instructions.md` | ~310 | Copilot-format project instructions |

### Tool-Specific Files

| Tool | File | Lines | Purpose |
|------|------|:-----:|---------|
| Copilot | `.github/extensions/*.mjs` (×7) | ~50 each | Custom tool extensions |
| Copilot | `.github/copilot-mcp.json` | ~30 | MCP server configuration |
| Copilot | `.github/lsp.json` | ~15 | Language server configuration |
| Copilot | `.github/copilot-setup-steps.yml` | ~40 | Cloud agent environment setup |
| Claude | `CLAUDE.md` | ~273 | Claude-specific reasoning patterns |
| Claude | `.claude/rules/*.md` (×10) | ~30 each | Glob-targeted contextual rules |
| Claude | `.claude/hooks/*.ps1` (×8) | ~40 each | Lifecycle event hooks |
| Claude | `.claude/skills/*/SKILL.md` (×43) | ~10 each | Skill bridge files |
| Claude | `.claude/settings.json` | ~50 | Hook registration and environment |
| Gemini | `GEMINI.md` | ~247 | Gemini-specific exploration patterns |
| Codex | `CODEX.md` | ~200 | Codex-specific autonomous guardrails |
| Codex | `.codex/config.toml` | ~20 | Model and approval configuration |

---

*This audit report was generated as part of the NexTruzt.io AI infrastructure hardening initiative. For questions or updates, see `.github/SETUP-GUIDE.md` or run the `planning_status` tool to check documentation freshness.*
