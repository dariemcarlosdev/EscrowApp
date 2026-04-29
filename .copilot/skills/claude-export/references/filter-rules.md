# Filter Rules Reference

## Purpose

Filtering enables selective exports: choosing which skills, bridges, and extensions to copy to project `.claude`. Useful for:
- Reducing project `.claude` size (export only needed skills)
- Bootstrap with curated set (security + testing + documentation)
- Multi-team setup (different skill sets per project)
- Compliance (exclude experimental features)

## Filter Types

### By Category

Categories match the `.github/skills/` directory structure:

| Category | Skills | Use For |
|---|---|---|
| `ai` | agent-orchestrator, mcp-developer, prompt-engineer | AI orchestration, LLM work |
| `code-quality` | code-reviewer, refactor-planner, debugging-wizard, etc. | Code review, refactoring, QA |
| `security` | owasp-audit, secret-scanner, threat-modeler, auth, authz | Security audits, OWASP compliance |
| `architecture` | architecture-reviewer, design-pattern-advisor, legacy-modernizer | Architecture reviews, design decisions |
| `testing` | test-generator, tdd-coach, test-coverage-analyzer | Test development, TDD |
| `database` | schema-reviewer, query-optimizer | Database work |
| `devops` | ci-cd-builder, deployment-preflight, monitoring-expert | CI/CD, deployment, observability |
| `documentation` | readme-generator, adr-creator, api-documenter | Documentation generation |
| `research` | codebase-explorer, tech-spike-planner, spec-miner | Investigation, research |
| `project-management` | spec-writer, issue-creator, feature-forge | Planning, task breakdown |
| `language` | dotnet-core-expert, csharp-developer | Language-specific |
| `workflow` | memory-optimization | Internal workflow |

**Usage:**
```json
{
  "filterBy": "category",
  "categories": ["ai", "security", "testing"]
}
```

### By Skill Name

Explicitly list skills to export.

**Usage:**
```json
{
  "filterBy": "name",
  "skills": ["claude-export", "agent-orchestrator", "owasp-audit"]
}
```

### By Pattern (Regex)

Use regex patterns to match skill names.

**Usage:**
```json
{
  "filterBy": "pattern",
  "patterns": ["^security.*", ".*testing.*", "agent.*"]
}
```

**Common patterns:**
- `^ai-.*` — All AI skills
- `.*security.*` — Any skill with "security" in name
- `^code-.*` — All code-quality skills
- `.*test.*` — Any testing-related skill

### By Metadata Tag

Filter by tags in skill metadata.

**Usage:**
```json
{
  "filterBy": "tag",
  "tags": ["security", "compliance", "fintech"]
}
```

**Metadata tags (in SKILL.md frontmatter):**
```yaml
metadata:
  tags: [security, fintech, payment-processing]
```

### By Domain

Filter by metadata domain field.

**Usage:**
```json
{
  "filterBy": "domain",
  "domains": ["ai", "security", "architecture"]
}
```

### By Platform

Include only skills for specific platforms.

**Usage:**
```json
{
  "filterBy": "platform",
  "platforms": ["copilot-cli", "claude"]
}
```

**Platform values:**
- `copilot-cli` — GitHub Copilot CLI
- `claude` — Claude IDE
- `gemini` — Gemini IDE
- `vscode` — VS Code
- `generic` — All platforms

## Combined Filters

Chain multiple filters (AND logic):

```json
{
  "filters": [
    { "type": "category", "values": ["security", "testing"] },
    { "type": "platform", "values": ["claude"] },
    { "type": "excludePatterns", "values": [".*deprecated.*"] }
  ]
}
```

**Logic:** 
```
(category in [security, testing])
AND (platform includes claude)
AND (NOT name matches deprecated)
```

## Exclusion Filters

Exclude items from export:

**Usage:**
```json
{
  "filterBy": "category",
  "categories": ["ai", "security", "testing"],
  "exclude": {
    "skills": ["old-skill", "deprecated-skill"],
    "patterns": [".*experimental.*", ".*preview.*"]
  }
}
```

## Preset Filter Sets

Pre-defined filter combinations:

### Preset: Security-Focused
```json
{
  "name": "security-focused",
  "description": "All security skills + architecture review",
  "filters": {
    "categories": ["security", "architecture"],
    "include": ["threat-modeler", "authentication", "authorization"]
  }
}
```

### Preset: Full Stack Development
```json
{
  "name": "full-stack-dev",
  "description": "AI, code quality, testing, database, devops",
  "filters": {
    "categories": ["ai", "code-quality", "testing", "database", "devops"]
  }
}
```

### Preset: Compliance & Audit
```json
{
  "name": "compliance-audit",
  "description": "Security, architecture, documentation for compliance",
  "filters": {
    "categories": ["security", "architecture", "documentation"],
    "tags": ["compliance", "audit", "owasp"]
  }
}
```

### Preset: Minimal (Bootstrap)
```json
{
  "name": "minimal",
  "description": "Just essential: code review, testing, security",
  "filters": {
    "skills": [
      "code-reviewer",
      "test-generator",
      "owasp-audit",
      "agent-orchestrator"
    ]
  }
}
```

## Filter Evaluation Algorithm

```
results = []

for each skill in global_skills:
  matched = false
  
  # Evaluate all filter criteria
  if filterBy == "category":
    matched = skill.category in filters.categories
  
  else if filterBy == "name":
    matched = skill.name in filters.skills
  
  else if filterBy == "pattern":
    for pattern in filters.patterns:
      if skill.name matches regex(pattern):
        matched = true
        break
  
  else if filterBy == "tag":
    matched = any(tag in skill.metadata.tags for tag in filters.tags)
  
  else if filterBy == "domain":
    matched = skill.metadata.domain in filters.domains
  
  # Apply exclusions
  if exclude.skills contains skill.name:
    matched = false
  
  for pattern in exclude.patterns:
    if skill.name matches regex(pattern):
      matched = false
      break
  
  if matched:
    results.append(skill)

return results
```

## Filter Configuration File

Save filter configurations in `.claude/filters.json`:

```json
{
  "version": "1.0.0",
  "presets": {
    "security-focused": {
      "description": "All security skills + architecture review",
      "categories": ["security", "architecture"],
      "include": ["threat-modeler", "authentication", "authorization"]
    },
    "full-stack-dev": {
      "description": "AI, code quality, testing, database, devops",
      "categories": ["ai", "code-quality", "testing", "database", "devops"]
    }
  },
  "activePreset": "full-stack-dev",
  "customFilters": {
    "myCompanySetup": {
      "description": "Skills required for MyCompany projects",
      "categories": ["ai", "security", "testing"],
      "exclude": {
        "patterns": [".*experimental.*"]
      }
    }
  }
}
```

## PowerShell Filter Functions

```powershell
function Test-SkillMatchesFilter {
  param(
    [object]$Skill,
    [hashtable]$FilterCriteria
  )
  
  $matched = $false
  
  # Category filter
  if ($FilterCriteria.FilterBy -eq "category") {
    $matched = $FilterCriteria.Categories -contains $Skill.category
  }
  
  # Name filter
  elseif ($FilterCriteria.FilterBy -eq "name") {
    $matched = $FilterCriteria.Skills -contains $Skill.name
  }
  
  # Pattern filter
  elseif ($FilterCriteria.FilterBy -eq "pattern") {
    foreach ($pattern in $FilterCriteria.Patterns) {
      if ($Skill.name -match $pattern) {
        $matched = $true
        break
      }
    }
  }
  
  # Apply exclusions
  if ($FilterCriteria.Exclude.Skills -contains $Skill.name) {
    $matched = $false
  }
  
  foreach ($pattern in $FilterCriteria.Exclude.Patterns) {
    if ($Skill.name -match $pattern) {
      $matched = $false
      break
    }
  }
  
  return $matched
}

function Get-FilteredSkills {
  param(
    [array]$AllSkills,
    [hashtable]$FilterCriteria
  )
  
  return $AllSkills | Where-Object { Test-SkillMatchesFilter $_ $FilterCriteria }
}

# Usage
$filters = @{
  FilterBy = "category"
  Categories = @("ai", "security")
  Exclude = @{
    Skills = @("deprecated-skill")
    Patterns = @(".*experimental.*")
  }
}

$filtered = Get-FilteredSkills -AllSkills $allSkills -FilterCriteria $filters
Write-Host "Matched $($filtered.Count) skills"
```

## Testing Filters

Before exporting, preview what will be exported:

```powershell
function Test-FilterPreview {
  param(
    [string]$GlobalPath,
    [hashtable]$FilterCriteria
  )
  
  # Discover all skills
  $allSkills = Get-ChildItem "$GlobalPath/skills" -Recurse -Filter "SKILL.md" | ForEach-Object {
    @{
      name = $_.Directory.Name
      category = $_.Directory.Parent.Name
      path = $_.FullName
    }
  }
  
  # Apply filter
  $filtered = Get-FilteredSkills -AllSkills $allSkills -FilterCriteria $FilterCriteria
  
  # Show results
  Write-Host "Filter Preview"
  Write-Host "=============="
  Write-Host "Criteria: $($FilterCriteria | ConvertTo-Json)"
  Write-Host "Matched: $($filtered.Count) / $($allSkills.Count) skills"
  Write-Host ""
  Write-Host "Selected:"
  $filtered | ForEach-Object { Write-Host "  • $($_.category)/$($_.name)" }
  
  return $filtered
}
```

