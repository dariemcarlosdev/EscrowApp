# Structure Conventions Reference

## Purpose

Claude Code has standardized conventions for `.claude` folder structure to ensure consistency, discoverability, and compatibility across projects and environments. This reference defines:

- **Standard conventions** — The "correct" folder structure
- **Compliance levels** — How to assess if a project meets conventions
- **Validation rules** — What to check during structure analysis
- **Refactoring guide** — How to migrate from non-compliant to compliant structure
- **Structure variants** — Supported alternative layouts for special cases

## Standard Claude Code Structure

The canonical `.claude` folder structure follows this pattern:

```
project/.claude/
├── manifest.json              # Inventory of skills, bridges, extensions
├── settings.json              # Claude IDE settings (optional)
├── CLAUDE.md                  # Claude model bridge (instructions for Claude)
├── GEMINI.md                  # Gemini model bridge (instructions for Gemini)
├── CODEX.md                   # Codex model bridge (instructions for Codex/Ollama)
├── OpenAI.md                  # OpenAI model bridge (instructions for GPT-4/etc)
│
├── skills/                    # Skills organized by category
│   ├── ai/
│   │   ├── agent-orchestrator/
│   │   │   ├── SKILL.md       # Main skill file (required)
│   │   │   └── references/    # Supporting documentation (optional)
│   │   │       ├── delegation-patterns.md
│   │   │       ├── context-minimization.md
│   │   │       └── ...
│   │   ├── claude-export/
│   │   ├── mcp-developer/
│   │   └── ...
│   │
│   ├── security/
│   │   ├── owasp-audit/
│   │   │   ├── SKILL.md
│   │   │   └── references/
│   │   ├── authentication/
│   │   └── ...
│   │
│   ├── architecture/
│   ├── code-quality/
│   ├── testing/
│   ├── database/
│   ├── devops/
│   ├── documentation/
│   ├── research/
│   ├── project-management/
│   ├── language/
│   └── workflow/
│
├── extensions/                # Custom extensions (optional)
│   ├── custom-extension-1/
│   │   ├── manifest.json
│   │   ├── index.js
│   │   └── ...
│   └── custom-extension-2/
│
└── .gitignore                 # Ignore rules (optional)
```

## Compliance Levels

### ✅ COMPLIANT

Project structure **fully adheres** to standard conventions.

**Characteristics:**
- Skills in `skills/{category}/{skillName}/SKILL.md` (nested by category)
- Bridges in root (`.claude/CLAUDE.md`, `.claude/GEMINI.md`, etc.)
- Extensions in `extensions/{extensionName}/`
- Settings in root (`manifest.json`, `settings.json`)
- No loose files outside conventions
- All required files present (SKILL.md)
- All reference files accessible

**Compliance check:**
```powershell
$project = ".claude"
$compliant = $true

# Check skills structure
$skills = Get-ChildItem "$project/skills" -Recurse -Filter "SKILL.md"
foreach ($skill in $skills) {
  $path = $skill.FullName
  # Should match: .claude/skills/{category}/{skillName}/SKILL.md
  if ($path -notmatch '\.claude[/\\]skills[/\\]\w+[/\\]\w+[/\\]SKILL\.md') {
    $compliant = $false
    Write-Host "❌ Non-standard skill path: $path"
  }
}

if ($compliant) {
  Write-Host "✅ COMPLIANT"
}
```

### ⚠️ PARTIAL

Project structure **mostly compliant** with minor deviations.

**Common deviations:**
1. **Flat skill structure** — Skills in `skills/{skillName}/` instead of `skills/{category}/{skillName}/`
2. **Missing references** — Skill exists but `references/` directory is missing (acceptable if skill has no references)
3. **Extra files** — Loose markdown files in `.claude/` root (e.g., `TODO.md`, `NOTES.md`)
4. **Custom naming** — Skill directories use different naming conventions (e.g., `agent_orchestrator` vs `agent-orchestrator`)
5. **Bridges in subdir** — Bridges in `bridges/` folder instead of root (old convention)

**Severity:**
- **LOW** — Extra files, custom naming (easily fixed, functionality unaffected)
- **MEDIUM** — Missing references (functionality works, docs incomplete)
- **MEDIUM** — Flat skills structure (works, but less organized, category discovery harder)

**Recovery:** Usually requires minor reorganization (moving directories, renaming files)

```powershell
# Example: Detect flat skills structure
$flatSkills = Get-ChildItem ".claude/skills" -MaxDepth 1 -Directory | 
  Where-Object { (Get-ChildItem $_.FullName -Filter "SKILL.md").Count -gt 0 }

if ($flatSkills) {
  Write-Host "⚠️ PARTIAL: Flat skills structure detected"
  Write-Host "  Found $($flatSkills.Count) skills in root of skills/"
  Write-Host "  Suggestion: Organize by category: skills/{category}/{skillName}/"
}
```

### ❌ NON-COMPLIANT

Project structure **significantly deviates** from standard conventions.

**Common violations:**
1. **Custom root structure** — Skills/bridges in completely custom locations (e.g., `my_claude/tools/`, `config/ai/`)
2. **Missing standard files** — No `SKILL.md`, no bridges, no manifest
3. **Wrong file format** — Skills as `.json` instead of `.md`, malformed YAML
4. **Broken references** — Skill points to missing reference files, circular dependencies
5. **No organization** — Everything in root `.claude/` directory

**Impact:** 
- Tools may not recognize skills
- IDE integration may fail
- Syncing/updates difficult or impossible
- Portability broken (can't share `.claude` config)

**Recovery:** Requires significant refactoring (directory restructure, file conversions)

```powershell
# Example: Detect non-compliant structure
$hasSkillsDir = Test-Path ".claude/skills"
$hasManifest = Test-Path ".claude/manifest.json"
$hasBridges = @("CLAUDE.md", "GEMINI.md") | 
  ForEach-Object { Test-Path ".claude/$_" } | 
  Where-Object { $_ } | 
  Measure-Object | 
  Select-Object -ExpandProperty Count

if (-not $hasSkillsDir -or -not $hasManifest -or $hasBridges -eq 0) {
  Write-Host "❌ NON-COMPLIANT: Missing critical structure"
  Write-Host "  Missing: $(if (-not $hasSkillsDir) { 'skills/ directory ' })"
  Write-Host "  Missing: $(if (-not $hasManifest) { 'manifest.json ' })"
  Write-Host "  Missing: $(if ($hasBridges -eq 0) { 'model bridges (CLAUDE.md, GEMINI.md)' })"
}
```

## Validation Algorithm

### Step 1: Structure Scan

```powershell
function Get-StructureProfile {
  param([string]$ProjectPath)
  
  $profile = @{
    hasSkillsDir = Test-Path "$ProjectPath/skills"
    hasExtensionsDir = Test-Path "$ProjectPath/extensions"
    hasManifest = Test-Path "$ProjectPath/manifest.json"
    hasSettings = Test-Path "$ProjectPath/settings.json"
    bridges = @()
    skills = @()
    looseFiles = @()
  }
  
  # Scan for bridges
  @("CLAUDE.md", "GEMINI.md", "CODEX.md", "OpenAI.md") | ForEach-Object {
    if (Test-Path "$ProjectPath/$_") {
      $profile.bridges += $_
    }
  }
  
  # Scan for skills
  if ($profile.hasSkillsDir) {
    Get-ChildItem "$ProjectPath/skills" -Recurse -Filter "SKILL.md" | ForEach-Object {
      $profile.skills += @{
        name = $_.Directory.Name
        path = $_.FullName
        category = $_.Directory.Parent.Name
      }
    }
  }
  
  # Scan for loose files
  Get-ChildItem "$ProjectPath" -File | Where-Object { $_.Extension -eq ".md" -or $_.Extension -eq ".json" } | ForEach-Object {
    if ($_.Name -notin @("README.md", ".gitignore", "package.json") -and $_.Name -notmatch "^(CLAUDE|GEMINI|CODEX|OpenAI)\.md$") {
      $profile.looseFiles += $_.Name
    }
  }
  
  return $profile
}
```

### Step 2: Compliance Assessment

```powershell
function Test-StructureCompliance {
  param([object]$Profile)
  
  $compliance = @{
    level = "compliant"  # compliant, partial, non-compliant
    score = 100
    issues = @()
    suggestions = @()
  }
  
  # Critical checks (make non-compliant)
  if (-not $Profile.hasSkillsDir) {
    $compliance.level = "non-compliant"
    $compliance.score -= 40
    $compliance.issues += "No skills/ directory"
    $compliance.suggestions += "Create skills/ directory and organize skills by category"
  }
  
  if (-not $Profile.hasManifest) {
    $compliance.level = "non-compliant"
    $compliance.score -= 30
    $compliance.issues += "No manifest.json"
    $compliance.suggestions += "Generate manifest.json with skill inventory"
  }
  
  if ($Profile.bridges.Count -eq 0) {
    $compliance.score -= 20
    $compliance.issues += "No model bridges (CLAUDE.md, GEMINI.md)"
    $compliance.suggestions += "Add at least CLAUDE.md and GEMINI.md"
  }
  
  # Skill structure checks
  $nestedSkills = $Profile.skills | Where-Object { $_.category -ne "skills" }
  $flatSkills = $Profile.skills | Where-Object { $_.category -eq "skills" }
  
  if ($flatSkills.Count -gt 0 -and $nestedSkills.Count -eq 0) {
    if ($compliance.level -eq "compliant") {
      $compliance.level = "partial"
    }
    $compliance.score -= 15
    $compliance.issues += "Flat skills structure (no category subdirectories)"
    $compliance.suggestions += "Organize skills: skills/{category}/{skillName}/SKILL.md"
  }
  
  # Loose files check
  if ($Profile.looseFiles.Count -gt 0) {
    if ($compliance.level -eq "compliant") {
      $compliance.level = "partial"
    }
    $compliance.score -= 5
    $compliance.issues += "Loose files in .claude root: $($Profile.looseFiles -join ', ')"
    $compliance.suggestions += "Move files to appropriate subdirectories or delete"
  }
  
  return $compliance
}
```

## Refactoring Guide

### Scenario 1: Flat Skills → Nested by Category

**Before:**
```
.claude/skills/
├── agent-orchestrator/SKILL.md
├── code-reviewer/SKILL.md
├── owasp-audit/SKILL.md
└── test-generator/SKILL.md
```

**After:**
```
.claude/skills/
├── ai/
│   ├── agent-orchestrator/SKILL.md
│   └── ...
├── security/
│   ├── owasp-audit/SKILL.md
│   └── ...
├── code-quality/
│   ├── code-reviewer/SKILL.md
│   └── ...
└── testing/
    ├── test-generator/SKILL.md
    └── ...
```

**Steps:**
```powershell
$skillsDir = ".claude/skills"

# Create category directories
$categories = @("ai", "security", "code-quality", "testing", "architecture", "database", "devops", "documentation", "research", "project-management", "language", "workflow")
$categories | ForEach-Object { New-Item -ItemType Directory -Path "$skillsDir/$_" -Force | Out-Null }

# Move skills to appropriate categories (requires skill -> category mapping)
$skillCategoryMap = @{
  "agent-orchestrator" = "ai"
  "code-reviewer" = "code-quality"
  "owasp-audit" = "security"
  "test-generator" = "testing"
  # ... add mappings for all skills
}

$skillCategoryMap.GetEnumerator() | ForEach-Object {
  $skill = $_.Key
  $category = $_.Value
  Move-Item "$skillsDir/$skill" "$skillsDir/$category/" -Force
}
```

### Scenario 2: Missing Bridges

**Before:** Only `.claude/CLAUDE.md` exists

**After:** Added `.claude/GEMINI.md`, `.claude/CODEX.md`

**Steps:**
```powershell
# Copy from global .claude if available
$globalClaude = "$HOME/.claude"
@("GEMINI.md", "CODEX.md") | ForEach-Object {
  if (Test-Path "$globalClaude/$_") {
    Copy-Item "$globalClaude/$_" ".claude/$_"
    Write-Host "✅ Added $_"
  } else {
    Write-Host "⚠️ Not found in global: $_"
    Write-Host "   Create manually or skip"
  }
}
```

### Scenario 3: Create Manifest from Discovered Skills

**Before:** No manifest.json

**After:** manifest.json with skill inventory

**Steps:**
```powershell
function Generate-Manifest {
  param([string]$ProjectPath, [object]$DiscoveredSkills)
  
  $manifest = @{
    version = "1.0.0"
    exportedAt = (Get-Date -AsUTC -Format "o")
    projectRoot = $ProjectPath
    items = $DiscoveredSkills
    summary = @{
      totalItems = $DiscoveredSkills.Count
      skillsCount = ($DiscoveredSkills | Where-Object { $_.type -eq "skill" }).Count
    }
  }
  
  $manifest | ConvertTo-Json -Depth 10 | Set-Content "$ProjectPath/.claude/manifest.json"
}
```

## Custom Structure Variants

In rare cases, projects may have legitimate reasons for custom structures (e.g., monorepos, legacy setups). **Document these explicitly** in a `.claude/STRUCTURE.md` file:

```markdown
# Project .claude Structure

## Custom Layout

This project uses a custom .claude structure:

```
.claude/
├── skills-ai/           (AI skills)
├── skills-security/     (Security skills)
├── integrations/        (Model bridges: CLAUDE.md, GEMINI.md)
└── config.json          (Project-specific config)
```

## Rationale

- **Monorepo**: Multiple .claude configs per workspace
- **Legacy**: Migrating from old structure gradually
- **Custom**: Specialized organization for this project type

## Migration Plan

Timeline to move to standard structure:
- Q2 2026: Move AI skills to skills/ai/
- Q3 2026: Move security skills to skills/security/
- Q4 2026: Full alignment with standard structure
```

