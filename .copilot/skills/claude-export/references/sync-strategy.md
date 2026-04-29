# Sync Strategy Reference

## Purpose

The sync strategy manages incremental updates between global and project-specific `.claude` directories, ensuring changes to global skills/bridges/extensions are reflected in the project while preserving local customizations.

## Sync Modes

### Full Sync
Replaces the entire project `.claude` with a fresh copy from global.

**Use when:**
- Project `.claude` doesn't exist yet (initial bootstrap)
- Project `.claude` is corrupted or out of date
- User explicitly requests "full refresh"
- No local customizations exist

**Steps:**
1. Back up existing project `.claude` (if exists)
2. Remove project `.claude` directory
3. Copy entire global `.claude` to project `.claude`
4. Generate fresh manifest
5. Validate integrity

**Commands:**
```powershell
# Back up existing
Move-Item .claude .claude.backup -Force

# Copy fresh
Copy-Item $GLOBAL_CLAUDE -Destination .claude -Recurse

# Validate
Test-ManifestIntegrity .claude/manifest.json
```

### Incremental Sync
Updates only changed items while preserving local customizations.

**Use when:**
- Project `.claude` already exists
- You want to update specific skills/bridges
- Local customizations need to be preserved
- Selective updates needed

**Steps:**
1. Load current project manifest
2. Load global manifest (or inventory)
3. Compare versions and checksums
4. For each item:
   - If global version > project version: UPDATE
   - If checksums match: SKIP
   - If local modified (checksum mismatch): MERGE or SKIP
5. Update project manifest
6. Log changes

**Algorithm:**
```
For each item in global:
  1. Check if exists in project
  
  if NOT EXISTS:
    → Add to project (new item)
  
  else:
    → Compare versions
    if global.version > project.version:
      → Check if local modified (compare project checksum vs source)
      if NOT modified locally:
        → Update (copy from global)
      else:
        → Prompt user (MERGE | SKIP | OVERWRITE)
    else:
      → SKIP (project is same or newer)
```

### Selective Sync
Update only specific categories or skills.

**Use when:**
- You want to update only security skills
- You want to add a specific skill from global
- You want to exclude certain items

**Scope parameters:**
```json
{
  "mode": "selective",
  "categories": ["ai", "security"],
  "skills": ["claude-export", "agent-orchestrator"],
  "bridges": ["CLAUDE.md"],
  "overwriteLocal": false
}
```

## Conflict Resolution

### Scenario 1: Global Version Newer, Local Not Modified

**Condition:** sourceChecksum == projectChecksum, but global.version > project.version

**Resolution:** AUTO UPDATE
```
Project ← Global (copy)
Update manifest version
Log: "Updated {name} from v{old} to v{new}"
```

### Scenario 2: Global Version Newer, Local Modified

**Condition:** sourceChecksum != projectChecksum

**Resolution:** USER CHOICE (or policy-based)

**Options:**
1. **MERGE** — Attempt 3-way merge (global, project, common base)
   - For text files (SKILL.md): merge tool
   - For JSON: deep merge settings
2. **SKIP** — Keep local version
   - Log: "Local modifications preserved in {name}"
3. **OVERWRITE** — Force global version
   - Log: "Local modifications overwritten with global v{version}"

**Prompt:**
```
⚠️ Conflict: claude-export

Global:   v1.1.0 (size: 12.5 KB)
Project:  v1.0.0 (size: 12.2 KB) [LOCALLY MODIFIED]

Options:
  [M] Merge (recommended)
  [S] Skip (keep local)
  [O] Overwrite with global
  [A] Auto (merge all conflicts)

Choice: _
```

### Scenario 3: Project Newer Than Global

**Condition:** project.version > global.version

**Resolution:** SKIP (project is ahead)
```
Log: "Project version {name} v{proj} newer than global v{global}, skipping"
```

### Scenario 4: Deleted in Global

**Condition:** Item exists in project but not in global

**Resolution:** USER CHOICE

**Options:**
1. **KEEP** — Preserve in project (item was removed from global)
2. **DELETE** — Remove from project (follow global)

**Prompt:**
```
⚠️ Orphan: old-skill (v1.0.0)

This skill exists in your project but not in the global .claude.
It may have been deprecated or moved.

Options:
  [K] Keep in project
  [D] Delete from project
  [I] Ignore

Choice: _
```

## Manifest Reconciliation

After sync, update the project manifest:

```powershell
function Update-Manifest {
  param(
    [string]$ProjectPath,
    [array]$UpdatedItems,
    [string]$SyncMode
  )
  
  $manifest = Get-Content "$ProjectPath/.claude/manifest.json" | ConvertFrom-Json
  
  foreach ($item in $UpdatedItems) {
    # Find and update existing entry
    $index = $manifest.items.IndexOf($manifest.items | Where-Object { $_.name -eq $item.name })
    
    if ($index -ge 0) {
      $manifest.items[$index].version = $item.version
      $manifest.items[$index].projectChecksum = $item.projectChecksum
      $manifest.items[$index].copiedAt = (Get-Date -AsUTC -Format "o")
    } else {
      # Add new item
      $manifest.items += $item
    }
  }
  
  # Update summary
  $manifest.summary.totalItems = $manifest.items.Count
  $manifest.summary.skillsCount = ($manifest.items | Where-Object { $_.type -eq "skill" }).Count
  $manifest.exportedAt = (Get-Date -AsUTC -Format "o")
  
  # Save updated manifest
  $manifest | ConvertTo-Json -Depth 10 | Set-Content "$ProjectPath/.claude/manifest.json"
}
```

## Sync Log Example

```
SYNC REPORT
===========

Mode:         incremental
Started:      2026-04-15T18:18:10Z
Completed:    2026-04-15T18:18:15Z

SUMMARY
-------
Checked:      42 items
Updated:      3 items
Skipped:      38 items
Conflicts:    1 item (merged)
Errors:       0

DETAILED LOG
------------
✅ agent-orchestrator (ai/skill)    v2.0.0 → v2.1.0   [updated]
⏭️ code-reviewer (code-quality/skill)           [skipped, already latest]
🔄 CLAUDE.md (bridge)                           [merged, local + global]
⚠️ old-deprecated-skill               [orphaned, kept in project]
✅ new-ai-skill (ai/skill)           v1.0.0    [added from global]

CONFLICTS RESOLVED
------------------
CLAUDE.md: 3-way merge (2 sections updated, 1 local section preserved)

NEXT STEPS
----------
1. Review merged files in project: git diff .claude/
2. Test updated skills: /agent-orchestrator (in Claude)
3. Delete orphaned skill if no longer needed: rm -r .claude/skills/...
4. Commit changes: git add .claude/ && git commit -m "Sync .claude from global"
```

## Best Practices

| Practice | Why | How |
|---|---|---|
| **Backup before full sync** | Recover from mistakes | `cp -r .claude .claude.backup` before sync |
| **Review conflicts before merge** | Preserve important local changes | Use `git diff` or manual review |
| **Version global skills/bridges** | Track what changed | Store version in metadata |
| **Document local customizations** | Explain why .claude differs from global | Add comments in CLAUDE.md, settings.json |
| **Commit .claude to version control** | Track history and allow rollback | `git add .claude/` after sync |
| **Sync before new feature work** | Ensure latest tools available | Run sync at start of session |
| **Selective syncs for safety** | Update high-value items without risk | Use `mode: selective` for critical projects |

