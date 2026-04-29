# Claude Export Skill

## Overview

The **claude-export** skill orchestrates the discovery, analysis, filtering, and export of `.claude` configuration from a global installation to project-specific directories with structure validation, convention compliance, and full integrity validation.

**Key innovation:** Analyzes if your project's `.claude` structure meets Claude Code conventions. If not, suggests refactoring. Then tailors the export to your project's structure during copy.

## Files

- **SKILL.md** — Core 6-step workflow and when to use this skill
- **references/project-structure-templates.md** — Where to place `.claude` in different project types (single, monorepo, workspace, enterprise)
- **references/structure-conventions.md** — Claude Code folder conventions, compliance levels, refactoring guides
- **references/discovery-pattern.md** — How to find global vs project `.claude` directories
- **references/manifest-schema.md** — Manifest format, generation, and validation
- **references/sync-strategy.md** — Incremental sync, conflict resolution, merge strategies
- **references/filter-rules.md** — Selective export by category, name, pattern, or tag
- **references/error-handling.md** — Recovery from permission, file, integrity, and validation errors

## Quick Start

1. **Choose location** → Read `project-structure-templates.md` (where should `.claude` go?)
2. **Check structure** → Read `structure-conventions.md` (is your structure compliant?)
3. **Follow workflow** → Read **SKILL.md** and follow the 6-step Core Workflow:
   - **Step 0:** Analyze project `.claude` structure (new!)
   - **Step 1:** Discover global and project directories
   - **Step 2:** Tailor export to project structure (new!)
   - **Step 3:** Determine export scope and filters
   - **Step 4:** Copy components and generate manifests
   - **Step 5:** Validate copy integrity and structure
   - **Step 6:** Summarize and report results

4. For specific sub-tasks, load the relevant reference:
   - Understanding manifests: → `manifest-schema.md`
   - Syncing updates: → `sync-strategy.md`
   - Filtering items: → `filter-rules.md`
   - Error recovery: → `error-handling.md`

## Use Cases

| Use Case | Workflow Steps | Key References |
|---|---|---|
| **New project setup** | Choose location (templates) → Validate structure → Steps 1-6 | project-structure-templates.md, structure-conventions.md |
| **Fix non-compliant structure** | Analyze structure → Follow refactoring plan → Steps 1-6 | structure-conventions.md |
| **Initial bootstrap** | All steps 0-6 (full export) | All references |
| **Selective export** | Step 2 (tailor), Step 3 (filter), Steps 4-6 | filter-rules.md |
| **Incremental sync** | Step 1 (discover), Step 3 (incremental mode), Steps 4-6 | sync-strategy.md |
| **Troubleshooting** | Step 5 (detailed validation), Step 6 (detailed report) | error-handling.md |

## Integration

**With Agent Orchestrator:** When using `agent-orchestrator` to delegate export work:
- Structure analysis → explore agent
- Copy & validate → general-purpose agent running Steps 4-5
- Report generation → reporting agent

**With MCP Developer:** If building MCP servers for `.claude` management, read `manifest-schema.md` for the data model.

**With Prompt Engineer:** When writing custom export scripts, reference all guides for algorithms and best practices.

## Version History

| Version | Release | Notes |
|---------|---------|-------|
| 2.0.0 | 2026-04-15 | Added structure validation (Step 0), project templates, structure-aware export (Step 2) |
| 1.0.0 | 2026-04-15 | Initial release: discovery, filtering, manifest generation, integrity validation |

## License

MIT

