---
name: claude-export
description: "Export and sync global .claude configuration structure to project-specific directories with full content, references, and metadata. Orchestrates discovery, filtering, copying, and manifest generation. Use when bootstrapping Claude IDE integration, migrating .claude configs between projects, or ensuring all Claude skills and bridges are available locally."
license: MIT
allowed-tools: Read, Grep, Glob, PowerShell, Create, Edit
metadata:
  version: "1.0.0"
  domain: ai
  triggers: .claude export, bootstrap Claude IDE, sync configuration, migrate settings, copy skills, setup bridges
  role: expert
  scope: setup
  platforms: copilot-cli, claude
  output-format: summary
  related-skills: agent-orchestrator, mcp-developer, codebase-explorer
---

# Claude Export Skill

An orchestration and configuration management tool that discovers the global `.claude` structure, exports specified components (skills, bridges, settings, extensions), and creates a complete, self-contained project-specific `.claude` directory with metadata, manifests, and cross-references — enabling seamless Claude IDE integration and configuration portability.

## When to Use This Skill

- Bootstrapping Claude IDE on a new project — need all skills/bridges available locally
- Migrating `.claude` configuration from one project to another
- Ensuring a project has its own copy of model bridges (CLAUDE.md, GEMINI.md, CODEX.md)
- Generating a `.claude` manifest for external sharing or backup
- Syncing updates from global `.claude` to project-specific `.claude`
- Comparing global vs project-specific versions to identify drift
- Exporting selective skills (e.g., only security skills) to a filtered `.claude`

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| **Project Structure Templates** | `references/project-structure-templates.md` | **NEW** — Before Step 0: Choosing where `.claude` should go in your project (single, monorepo, workspace, enterprise) |
| **Structure Conventions** | `references/structure-conventions.md` | Step 0: Validating project structure, understanding compliance levels, refactoring suggestions |
| Discovery Pattern | `references/discovery-pattern.md` | Step 1: Understanding how to find global vs project `.claude` |
| Manifest Schema | `references/manifest-schema.md` | Step 4: Creating/updating `.claude/manifest.json` with structure metadata |
| Sync Strategy | `references/sync-strategy.md` | Managing incremental updates while respecting structure |
| Filter Rules | `references/filter-rules.md` | Step 3: Selective skill/bridge export by category, name, or pattern |
| Error Handling | `references/error-handling.md` | Troubleshooting structure validation, copy failures, integrity issues |

## Core Workflow

### Step 0 — Validate Project .claude Structure (New Analysis Phase)

Analyze if the project's `.claude` folder structure meets Claude Code conventions. Suggest refactoring if non-compliant. If compliant, proceed with structure-aware export.

1. **Scan project .claude** — Does it exist? If not, skip to Step 1 (fresh setup).
2. **Check folder structure** — Validate against Claude Code conventions (see reference: `structure-conventions.md`)
3. **Assess compliance** — Is structure compliant, partially compliant, or non-compliant?
4. **Identify violations** — List any deviations from standard patterns
5. **Generate refactoring plan** (if needed) — Suggest folder reorganization, renames, consolidations
6. **Recommend actions** — Proceed with export as-is, or refactor first?

**Structure Validation Rules** (load from `structure-conventions.md`):
- Skills in `skills/{category}/{skillName}/`
- Bridges at root level (e.g., `.claude/CLAUDE.md`)
- Extensions in `extensions/{extensionName}/`
- Settings in root as `.json` files
- Each skill has `SKILL.md` + optional `references/` directory
- No loose files outside these directories
- No circular references or missing required files

**Compliance Levels:**
```
✅ COMPLIANT       — Meets all conventions, safe to export
⚠️  PARTIAL        — Meets most conventions, some minor issues (can export with warnings)
❌ NON-COMPLIANT   — Significant violations, recommend refactor before export
```

**✅ Checkpoint: Project structure analyzed, compliance level determined, refactoring plan (if needed) documented.**

### Step 1 — Discover Global and Project .claude Structures

Identify the source (global) and destination (project-specific) `.claude` directories.

1. **Locate global .claude** — Typically: `~/.claude/` or `C:\Users\{user}\.claude\`
2. **Locate project .claude** — Typically: `{projectRoot}/.claude/` or `{projectRoot}/.gemini/` etc.
3. **Scan source structure** — List all skills, bridges, extensions, settings in global `.claude`
4. **Inventory destination** — Check what already exists in project `.claude` (if present)
5. **Identify gaps** — Which items in global are missing from project?
6. **Check version mismatches** — Compare metadata versions between global and project (if both exist)

**Discovery commands:**
```powershell
# Find global .claude
Test-Path $HOME\.claude -or Test-Path $HOME\AppData\Local\.claude
Get-ChildItem -Recurse $HOME\.claude | Select FullName, LastWriteTime

# Find project .claude
Get-ChildItem -Path . -Filter .claude -Recurse -Directory
```

**✅ Checkpoint: Global and project `.claude` paths identified, inventory complete, gaps documented.**

### Step 2 — Tailor Export to Project Structure

Based on structure analysis from Step 0, customize the export to match the project's conventions.

1. **If COMPLIANT** — Export follows detected structure exactly
2. **If PARTIAL** — Export follows detected structure, flag warnings for minor issues
3. **If NON-COMPLIANT** — Offer refactor, or export with structure conversion
4. **Determine target directories** — Where will exported skills/bridges/extensions go?
5. **Define naming strategy** — Will names be normalized to conventions or kept as-is?

**Structure-Aware Export Decision:**
```
Project structure: .claude/skills/{category}/{skillName}/SKILL.md (✅ COMPLIANT)
  → Export matches this structure exactly

Project structure: .claude/skills/{skillName}/SKILL.md (⚠️ PARTIAL - no category dirs)
  → Export option:
    [A] Keep flat structure (export to .claude/skills/{skillName}/)
    [B] Refactor to nested (create categories and reorganize)

Project structure: .claude/custom_skills/{name}.md (❌ NON-COMPLIANT)
  → Export options:
    [A] Convert to standard structure: .claude/skills/{category}/{name}/SKILL.md
    [B] Keep custom structure, place exports in custom_skills/
    [C] Manual setup required (user will organize after export)
```

**✅ Checkpoint: Export scope tailored to project structure, directory mappings defined, naming strategy chosen.**

### Step 3 — Determine Export Scope

Decide what to export: all components, filtered set (by category/name), or incremental sync.

1. **Get user intent** — Full export? Selective (e.g., only security skills)? Incremental (update only)?
2. **Define filter criteria** — By skill name pattern, category (ai, security, architecture), or explicit list
3. **Calculate export size** — Count items, estimate copy time
4. **Validate permissions** — Can user read from global `.claude`? Can user write to project `.claude`?
5. **Plan overwrite strategy** — Merge, replace, or skip existing?

**Scope template:**
```json
{
  "exportType": "full|selective|incremental",
  "filterBy": "category|name|explicit-list",
  "filters": ["ai", "security"],
  "overwriteExisting": true|false,
  "includeMetadata": true
}
```

**✅ Checkpoint: Export scope defined, filters applied, permissions verified, overwrite strategy chosen.**

### Step 4 — Copy Components and Generate Manifests

Execute the copy operation with metadata tracking and manifest generation, respecting project structure.

1. **Create directories per detected structure** — Use folders determined in Step 2
2. **Copy selected items** — Skills, bridges, extensions, settings respecting the filter criteria AND structure
3. **Preserve metadata** — Copy .d.ts files, package.json, LICENSE files, README files
4. **Generate or update manifest** — Create `.claude/manifest.json` with inventory, versions, checksums, **structure version**
5. **Create sync log** — Record source, destination, timestamps, items copied, structure used

**Copy operation:**
```
For each filtered item in global .claude:
  1. Read item (directory or file)
  2. Resolve relative paths and references
  3. Copy to project .claude/{same-path}
  4. Update manifest entry with: name, version, category, copySource, copyTime, checksum
```

**Manifest structure:**
```json
{
  "version": "1.0.0",
  "exportedAt": "2026-04-15T18:18:10Z",
  "sourceGlobal": "/Users/{user}/.claude",
  "projectRoot": "/path/to/project",
  "items": [
    {
      "type": "skill",
      "name": "claude-export",
      "category": "ai",
      "version": "1.0.0",
      "path": "skills/ai/claude-export/SKILL.md",
      "sourceChecksum": "sha256:...",
      "copiedAt": "2026-04-15T18:18:10Z"
    }
  ],
  "summary": {
    "totalItems": 42,
    "skillsCount": 30,
    "bridgesCount": 4,
    "extensionsCount": 3,
    "settingsCount": 5
  }
}
```

**Manifest structure (updated to track structure version):**
```json
{
  "version": "1.0.0",
  "structureVersion": "claude-standard-v1",
  "structureCompliance": "compliant|partial|non-compliant",
  "exportedAt": "2026-04-15T18:18:10Z",
  "sourceGlobal": "/Users/{user}/.claude",
  "projectRoot": "/path/to/project",
  "projectStructure": {
    "skillsPath": "skills/{category}/{name}",
    "bridgesPath": "{name}.md",
    "extensionsPath": "extensions/{name}",
    "settingsPath": "{name}.json"
  },
  "items": [ ... ]
}
```

**✅ Checkpoint: All items copied using project structure, manifest generated with structure metadata, sync log complete.**

### Step 5 — Validate Copy Integrity and Structure

1. **Checksum validation** — Compare source and destination file hashes
2. **Completeness check** — All referenced files present (skills with references/, bridges with docs/, etc.)
3. **Manifest consistency** — Manifest entries match actual files on disk
4. **Cross-reference validation** — Links in SKILL.md point to existing reference files
5. **Metadata validation** — YAML frontmatter in SKILL.md is well-formed

**Validation rules:**
- Every skill referenced in manifest must have a `.github/skills/{category}/{name}/SKILL.md` file
- Every reference file mentioned in SKILL.md must exist on disk
- Bridge files (CLAUDE.md, GEMINI.md) must be parseable YAML/markdown
- All JSON files (manifest, settings) must be valid JSON

Verify that all copied items are complete, consistent, AND match the project's structure conventions.

1. **Checksum validation** — Compare source and destination file hashes
2. **Completeness check** — All referenced files present (skills with references/, bridges with docs/, etc.)
3. **Structure validation** — Items placed in correct directories per project structure
4. **Manifest consistency** — Manifest entries match actual files on disk
5. **Cross-reference validation** — Links in SKILL.md point to existing reference files
6. **Metadata validation** — YAML frontmatter in SKILL.md is well-formed
7. **Structure compliance check** — Are copied items in proper directories? Any naming violations?

**Structure Validation Rules:**
- Skills in `{projectStructure.skillsPath}` (e.g., `.claude/skills/ai/agent-orchestrator/`)
- Bridges in `{projectStructure.bridgesPath}` (e.g., `.claude/CLAUDE.md`)
- Extensions in `{projectStructure.extensionsPath}` (e.g., `.claude/extensions/custom-ext/`)
- Each skill has required `SKILL.md` file
- Reference files in `references/` subdirectory of skill
- No stray files outside conventions

**✅ Checkpoint: All checksums match, completeness verified, structure validation passed, cross-references valid, manifest consistent.**

### Step 6 — Summarize and Report Results

Generate a summary report with export statistics, warnings, structure analysis, and next steps.

1. **Structure analysis report** — Compliance level, violations found, refactoring performed
2. **Export statistics** — Total items, breakdown by type (skills/bridges/extensions), total size
3. **Structure applied** — Which folder structure was used during export
4. **Warnings** — Any checksums that don't match, missing reference files, validation errors, structure issues
5. **Next steps** — How to use the exported `.claude` (add to .gitignore, commit to repo, etc.)
6. **Rollback instructions** — How to revert if something went wrong

**Report template (updated):**
```
CLAUDE EXPORT REPORT
====================

Source:       {globalPath}
Destination:  {projectPath}
Exported At:  {timestamp}

STRUCTURE ANALYSIS
------------------
Compliance:   ✅ COMPLIANT
Structure:    .claude/skills/{category}/{name}/SKILL.md
Violations:   0

Refactoring Performed: None (structure already optimal)

SUMMARY
-------
Total Items Exported:    42
  Skills:                30
  Bridges:               4
  Extensions:            3
  Settings:              5

Items Skipped:           0
Items with Warnings:     0
Total Size:              ~12 MB

ITEMS COPIED
------------
✅ claude-export (ai/skill, v1.0.0)
   → .claude/skills/ai/claude-export/
✅ agent-orchestrator (ai/skill, v2.0.0)
   → .claude/skills/ai/agent-orchestrator/
✅ CLAUDE.md (bridge)
   → .claude/CLAUDE.md
... (list all items)

STRUCTURE VALIDATION
--------------------
✅ All skills in skills/{category}/{name}/ format
✅ All bridges at .claude/{name}.md level
✅ All extensions in extensions/{name}/ format
✅ All settings in .claude/{name}.json format

NO ERRORS

NEXT STEPS
----------
1. Commit .claude/ to version control: git add .claude/
2. Update .gitignore to allow .claude/: echo ".claude/" >> .gitignore
3. Test skill loading in Claude IDE: Open command palette → Search "claude-export"
4. For incremental updates in future: Use claude-export with scope=incremental

ROLLBACK
--------
To revert this export:
  rm -r {projectPath}/.claude
  git restore .claude/  (if previously committed)
```

**✅ Checkpoint: Report generated, structure analysis included, all warnings documented, next steps provided.**

---

## Integration with Agent Orchestrator

When using this skill as part of a multi-agent workflow (e.g., "bootstrap Claude for project X"):

1. **Agent Orchestrator** decomposes: discover → filter → copy → validate → report
2. **This skill** executes: Steps 1-5 as a coherent orchestration
3. **Result aggregation** collects manifests and reports from parallel agents (if filtering by category)

Example delegation:
```
User: "Bootstrap Claude skills for this project, but only security and testing skills"

Agent Orchestrator plans:
  - Skill discovery (explore agent)
  - Filter by security + testing (part of Step 2)
  - Copy and validate (general-purpose agent running this skill Steps 3-4)
  - Generate report (report generation agent)

Result: project/.claude with only security and testing skills, manifest, validation report
```

---

## Error Handling Strategy

| Error | Root Cause | Recovery |
|---|---|---|
| Global `.claude` not found | User hasn't run Claude IDE setup | Provide instructions for global `.claude` location |
| Permission denied on read | User lacks read permission to global `.claude` | Check file permissions, suggest sudo or retry |
| Project `.claude` write failure | Insufficient permissions or disk space | Check disk space, verify write permissions on project root |
| Checksum mismatch | File corrupted during copy or source changed | Log mismatch, offer manual verification, skip item or retry |
| Reference file missing | Source skill has broken references | Validate source skill first, report to skill author |
| Invalid YAML in SKILL.md | Malformed frontmatter | Skip item or offer to fix automatically |
| Duplicate items | Item exists in both global and filtered set | Merge metadata or prompt user for resolution |
