# Error Handling Reference

## Purpose

Comprehensive error recovery and validation strategy for the claude-export skill, ensuring robust operation even when encountering missing files, permission issues, corrupted data, or network failures.

## Error Categories

### Permission Errors

#### A. Read Permission Denied (Global .claude)

**Cause:** User lacks read access to global `.claude`

**Impact:** Cannot discover or copy items

**Recovery:**
```powershell
try {
  Test-Path $globalPath -ErrorAction Stop
  Get-ChildItem $globalPath -ErrorAction Stop | Out-Null
} catch {
  Write-Error "❌ Cannot read global .claude at $globalPath"
  Write-Host "Solution:"
  Write-Host "  1. Check path: ls -la $globalPath"
  Write-Host "  2. Fix permissions: chmod 755 $globalPath"
  Write-Host "  3. Retry export"
  exit 1
}
```

**User-facing:**
```
❌ ERROR: Permission Denied

Cannot read global .claude directory:
  Path: {path}
  Error: Access Denied

Your user may not have read permissions. Try:
  • Run as administrator (Windows): Run PowerShell as Admin
  • Check file permissions: ls -la {path}
  • Verify ownership: whoami

If issue persists, reinstall Claude IDE.
```

#### B. Write Permission Denied (Project .claude)

**Cause:** User lacks write access to project directory

**Impact:** Cannot create/update project `.claude`

**Recovery:**
```powershell
try {
  $testFile = Join-Path $projectRoot ".claude.test"
  "test" | Set-Content $testFile -ErrorAction Stop
  Remove-Item $testFile -ErrorAction Stop
} catch {
  Write-Error "❌ Cannot write to project directory $projectRoot"
  Write-Host "Solution:"
  Write-Host "  1. Check disk space: df -h {projectRoot}"
  Write-Host "  2. Check permissions: chmod 755 {projectRoot}"
  Write-Host "  3. Try from different directory"
  exit 1
}
```

**User-facing:**
```
❌ ERROR: Cannot Write to Project

Cannot create or update .claude in:
  Path: {projectRoot}

Possible causes:
  • Insufficient disk space
  • Permission denied (insufficient write access)
  • File system is read-only
  • Project directory doesn't exist

Try:
  • Check disk space: df -h
  • Change directory permissions: chmod 755 .
  • Create project directory manually
  • Run as administrator
```

### File Not Found Errors

#### C. Global .claude Not Found

**Cause:** User hasn't run Claude IDE setup, or path is wrong

**Impact:** Export cannot proceed

**Recovery:**
```powershell
function Find-GlobalClaude {
  $possiblePaths = @(
    "$HOME\.claude",
    "$HOME\AppData\Roaming\.claude",
    "$HOME\AppData\Local\.claude",
    "$PROFILE\..\\..\\.claude",
    "~/.claude"
  )
  
  foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
      return $path
    }
  }
  
  return $null
}

$globalPath = Find-GlobalClaude
if (-not $globalPath) {
  Write-Error "Global .claude not found in standard locations"
  Write-Host "Setup instructions:"
  Write-Host "  1. Install Claude IDE: https://claude.ai/download"
  Write-Host "  2. Run Claude and open a project"
  Write-Host "  3. Claude will auto-create ~/.claude"
  Write-Host "  4. Retry export"
  exit 1
}
```

**User-facing:**
```
❌ ERROR: Global .claude Not Found

Claude IDE hasn't been initialized yet. The global .claude directory
is created automatically when you first run Claude IDE.

Fix this:
  1. Download Claude IDE: https://claude.ai/download
  2. Install and launch Claude
  3. Open a project (File → Open)
  4. Wait for initialization (creates ~/.claude)
  5. Retry: claude-export
```

#### D. Project .claude Doesn't Exist (Initial Setup)

**Cause:** First-time setup for this project

**Impact:** Create new directory and copy from global

**Recovery:** AUTO (not an error, expected condition)
```powershell
if (-not (Test-Path ".claude")) {
  Write-Host "📁 Creating .claude directory..."
  New-Item -ItemType Directory -Path ".claude" -Force | Out-Null
  Write-Host "✅ Created .claude"
}
```

#### E. Reference File Missing

**Cause:** Skill has broken reference (references/file.md doesn't exist)

**Impact:** Skill may not work correctly; manifest validation fails

**Recovery:**
```powershell
function Validate-SkillReferences {
  param([string]$SkillPath)
  
  $skillFile = Join-Path $SkillPath "SKILL.md"
  $refDir = Join-Path $SkillPath "references"
  
  if (-not (Test-Path $skillFile)) {
    return "SKILL.md not found"
  }
  
  $content = Get-Content $skillFile -Raw
  
  # Find all "references/*.md" mentions
  $matches = [regex]::Matches($content, 'references/([^/\s\.]+\.md)')
  
  $missingRefs = @()
  foreach ($match in $matches) {
    $refFile = Join-Path $refDir $match.Groups[1].Value
    if (-not (Test-Path $refFile)) {
      $missingRefs += $match.Groups[1].Value
    }
  }
  
  return $missingRefs
}

$missing = Validate-SkillReferences ".claude/skills/ai/claude-export"
if ($missing) {
  Write-Warning "⚠️ Missing reference files in claude-export:"
  $missing | ForEach-Object { Write-Host "   • $_" }
  Write-Host "Action: Skill may still work, but documentation incomplete"
}
```

**User-facing:**
```
⚠️ WARNING: Incomplete Skill

Skill 'claude-export' references documentation that wasn't exported:
  Missing: references/sync-strategy.md
  Missing: references/error-handling.md

The skill may still work, but reference documentation is incomplete.
Options:
  [I] Ignore (continue)
  [R] Retry (re-download from global)
  [D] Delete (remove incomplete skill)

Choice: _
```

### Data Integrity Errors

#### F. Checksum Mismatch

**Cause:** File corrupted during copy or source changed after copy started

**Impact:** Integrity validation fails; item marked as out-of-sync

**Recovery:**
```powershell
function Repair-ChecksumMismatch {
  param([string]$ItemPath, [string]$SourcePath)
  
  Write-Host "🔧 Repairing: $ItemPath"
  
  # Recalculate checksums
  $sourceHash = (Get-FileHash $SourcePath -Algorithm SHA256).Hash
  $itemHash = (Get-FileHash $ItemPath -Algorithm SHA256).Hash
  
  if ($sourceHash -ne $itemHash) {
    Write-Host "⚠️ Checksums don't match (file changed)"
    Write-Host "  Source: $sourceHash"
    Write-Host "  Item:   $itemHash"
    
    # User choice
    $choice = Read-Host "  [U]pdate from source, [K]eep local, [Q]uit?"
    
    switch ($choice) {
      "U" {
        Copy-Item $SourcePath $ItemPath -Force
        Write-Host "  ✅ Updated from source"
      }
      "K" {
        Write-Host "  ✅ Kept local version"
      }
      "Q" {
        exit 0
      }
    }
  } else {
    Write-Host "✅ Checksums now match"
  }
}
```

**User-facing:**
```
⚠️ WARNING: Integrity Check Failed

File checksum mismatch:
  Item: .claude/skills/ai/claude-export/SKILL.md
  Expected: sha256:a1b2c3d4...
  Actual:   sha256:x9y8z7w6...

This could mean:
  • File was corrupted during copy
  • File was modified locally after export
  • Disk error

Options:
  [U] Re-copy from global .claude
  [K] Keep current version
  [V] View differences (diff)
  [Q] Quit

Choice: _
```

#### G. Invalid YAML in SKILL.md

**Cause:** Malformed frontmatter; cannot parse metadata

**Impact:** Manifest generation fails; skill not recognized

**Recovery:**
```powershell
function Validate-SkillYaml {
  param([string]$FilePath)
  
  $content = Get-Content $FilePath -Raw
  
  # Extract YAML block
  if ($content -match '(?s)^---(.*?)---') {
    $yaml = $matches[1]
    
    try {
      # Attempt to parse (this would use a YAML library)
      $parsed = $yaml | ConvertFrom-Yaml
      return @{ valid = $true; data = $parsed }
    } catch {
      return @{ valid = $false; error = $_.Message }
    }
  }
  
  return @{ valid = $false; error = "No YAML frontmatter found" }
}

$result = Validate-SkillYaml ".claude/skills/ai/claude-export/SKILL.md"
if (-not $result.valid) {
  Write-Warning "⚠️ Invalid YAML in SKILL.md:"
  Write-Host "  Error: $($result.error)"
  Write-Host "  Fix: Edit frontmatter and correct syntax"
  exit 1
}
```

**User-facing:**
```
❌ ERROR: Malformed Skill File

File has invalid YAML frontmatter:
  Path: .claude/skills/ai/claude-export/SKILL.md
  Error: Unexpected token at line 5: "description"

Skill files must start with:
  ---
  name: skill-name
  description: "..."
  license: MIT
  ...
  ---

Edit the file to fix YAML syntax, or re-export from global.
```

### Network/Copy Errors

#### H. Partial Copy / Incomplete Directory

**Cause:** Copy operation interrupted (disk full, network timeout, user cancelled)

**Impact:** Project `.claude` is incomplete; may cause errors

**Recovery:**
```powershell
function Test-CopyCompleteness {
  param([string]$ProjectPath, [object]$Manifest)
  
  $issues = @()
  
  foreach ($item in $manifest.items) {
    $fullPath = Join-Path $ProjectPath $item.path
    
    if (-not (Test-Path $fullPath)) {
      $issues += @{
        item = $item.name
        issue = "File missing"
        path = $item.path
      }
    } elseif ($item.type -eq "skill") {
      # Check for required reference files
      $refDir = (Split-Path $fullPath -Parent) + "/references"
      if (-not (Test-Path $refDir)) {
        $issues += @{
          item = $item.name
          issue = "References directory missing"
          path = $refDir
        }
      }
    }
  }
  
  return $issues
}

$issues = Test-CopyCompleteness ".claude" $manifest
if ($issues) {
  Write-Host "⚠️ Incomplete Export Detected:"
  $issues | ForEach-Object {
    Write-Host "  • $($_.item): $($_.issue)"
  }
  
  $choice = Read-Host "  [R]etry copy, [D]elete incomplete, [Q]uit?"
  
  switch ($choice) {
    "R" { Write-Host "  Retrying..."; Start-Sleep -Seconds 2 }
    "D" { Remove-Item ".claude" -Recurse -Force }
    "Q" { exit 0 }
  }
}
```

**User-facing:**
```
⚠️ WARNING: Incomplete Export

The export was interrupted. Some files are missing:
  Missing: .claude/skills/ai/agent-orchestrator/SKILL.md
  Missing: .claude/skills/ai/agent-orchestrator/references/

Options:
  [R] Retry export
  [D] Delete incomplete .claude and start over
  [Q] Quit

Choice: _
```

### Validation Errors

#### I. Manifest Inconsistency

**Cause:** Manifest doesn't match actual files on disk

**Impact:** Manifest cannot be trusted; sync operations will fail

**Recovery:**
```powershell
function Repair-Manifest {
  param([string]$ProjectPath)
  
  $manifestPath = Join-Path $ProjectPath ".claude/manifest.json"
  $claudeDir = Join-Path $ProjectPath ".claude"
  
  # Rediscover all items
  $discovered = @()
  
  Get-ChildItem "$claudeDir/skills" -Recurse -Filter "SKILL.md" | ForEach-Object {
    $category = $_.Directory.Parent.Name
    $skillName = $_.Directory.Name
    $discovered += @{
      type = "skill"
      name = $skillName
      category = $category
      path = "skills/$category/$skillName/SKILL.md"
    }
  }
  
  # Rebuild manifest
  $newManifest = @{
    version = "1.0.0"
    exportedAt = (Get-Date -AsUTC -Format "o")
    projectRoot = $ProjectPath
    items = $discovered
    summary = @{
      totalItems = $discovered.Count
      skillsCount = ($discovered | Where-Object { $_.type -eq "skill" }).Count
    }
  }
  
  $newManifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
  Write-Host "✅ Manifest repaired and rebuilt"
}
```

**User-facing:**
```
⚠️ WARNING: Manifest Out of Sync

The .claude/manifest.json doesn't match actual files on disk.

Discrepancies:
  • Manifest lists: 42 items
  • Actual files: 40 items
  • Missing from disk: old-skill, deprecated-skill

Options:
  [R] Rebuild manifest from actual files
  [M] Merge (add missing items to manifest)
  [Q] Quit

Choice: _
```

## Error Recovery Policy

| Error Severity | Action | User Notification |
|---|---|---|
| **CRITICAL** | Exit, do not proceed | ❌ ERROR (bold red) |
| **HIGH** | Prompt user for choice | ⚠️ WARNING (yellow) |
| **MEDIUM** | Log warning, continue | ℹ️ INFO (blue) |
| **LOW** | Log info, continue silently | (none) |

## Success/Failure Signals

### Success Indicators
```powershell
✅ Export completed successfully
✅ All items copied
✅ Manifest generated
✅ Integrity validation passed
✅ Ready to use
```

### Partial Success (Warnings)
```powershell
⚠️ 3 items skipped due to permission issues
⚠️ 1 skill has missing reference files
ℹ️ Export completed with warnings (see above)
```

### Failure States
```powershell
❌ FATAL: Cannot read global .claude
❌ FATAL: Cannot write to project directory
❌ FATAL: No items matched filter criteria
❌ Export failed. Use --verbose for details.
```

