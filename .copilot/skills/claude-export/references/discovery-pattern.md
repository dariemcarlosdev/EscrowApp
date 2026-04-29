# Discovery Pattern Reference

## Global vs Project .claude Locations

### On Windows
```
Global:       C:\Users\{username}\.claude\
Project:      C:\path\to\project\.claude\ or C:\path\to\project\.gemini\ or C:\path\to\project\.claude-ai\

Command to find:
  Get-ChildItem $HOME\.claude -ErrorAction SilentlyContinue
  Get-ChildItem . -Filter .claude -Recurse -Directory
```

### On macOS / Linux
```
Global:       ~/.claude/ or $HOME/.claude/
Project:      ./claude/ or ./.gemini/ or ./project/.claude-ai/

Command to find:
  ls -la ~/.claude 2>/dev/null
  find . -maxdepth 2 -name ".claude" -o -name ".gemini" | head -5
```

## Global .claude Structure

Expected layout:

```
~/.claude/
├── README.md                 # Global .claude documentation
├── manifest.json            # Manifest of available skills/bridges
├── settings.json            # Global Claude IDE settings
├── skills/
│   ├── ai/
│   │   ├── agent-orchestrator/
│   │   │   ├── SKILL.md
│   │   │   └── references/
│   │   ├── claude-export/
│   │   │   ├── SKILL.md
│   │   │   └── references/
│   │   └── ...
│   ├── security/
│   ├── architecture/
│   └── ...
├── bridges/
│   ├── CLAUDE.md            # Claude model bridge
│   ├── GEMINI.md            # Gemini model bridge
│   ├── CODEX.md             # Codex model bridge
│   └── OpenAI.md
├── extensions/
│   ├── custom-extension-1/
│   ├── custom-extension-2/
│   └── ...
└── .d.ts                     # TypeScript definitions for IDE
```

## Discovery Algorithm

### 1. Locate Global .claude

```
For Windows:
  1. Check: Test-Path $HOME\.claude
  2. Check: Test-Path $HOME\AppData\Roaming\.claude
  3. Check: Test-Path $PROFILE\..\\..\\.claude
  4. If not found: "Global .claude not found. Run Claude IDE setup first."

For macOS/Linux:
  1. Check: test -d ~/.claude
  2. Check: test -d $XDG_CONFIG_HOME/.claude
  3. If not found: "Global .claude not found. Run Claude IDE setup first."
```

### 2. Locate Project .claude

```
Start from project root, search upward:
  1. Does ./.claude exist?
  2. Does ./.gemini exist?
  3. Does ./.vscode/claude-config exist?
  4. Does .claude-ai/ exist?
  5. If none found: Create ./.claude/
```

### 3. Inventory Global Structure

```powershell
$globalPath = "$HOME\.claude"

$inventory = @{
  skills = @()
  bridges = @()
  extensions = @()
  settings = @()
}

# Scan skills
Get-ChildItem "$globalPath/skills" -Recurse -Filter "SKILL.md" | ForEach-Object {
  $category = $_.Directory.Parent.Name
  $skillName = $_.Directory.Name
  $inventory.skills += @{
    name = $skillName
    category = $category
    version = (Get-Content $_ | Select-String "version:" | Select-Object -First 1).Line
    path = $_.FullName
  }
}

# Scan bridges
Get-ChildItem "$globalPath/*.md" | Where-Object { $_.BaseName -in @("CLAUDE", "GEMINI", "CODEX") } | ForEach-Object {
  $inventory.bridges += @{
    name = $_.BaseName
    path = $_.FullName
  }
}

# Scan extensions
Get-ChildItem "$globalPath/extensions" -Directory | ForEach-Object {
  $inventory.extensions += @{
    name = $_.Name
    path = $_.FullName
  }
}

# Scan settings
Get-ChildItem "$globalPath/*.json" | ForEach-Object {
  $inventory.settings += @{
    name = $_.BaseName
    path = $_.FullName
  }
}

return $inventory
```

### 4. Compare Global vs Project

```
For each item in global:
  - Does it exist in project?
    - If YES: Check versions match. If NO: Schedule for update.
    - If NO: Schedule for copy.

Build list of:
  ✅ Items to copy (new)
  🔄 Items to update (version mismatch)
  ⏭️ Items to skip (already match)
```

## Metadata Extraction

Extract metadata from SKILL.md YAML frontmatter:

```yaml
---
name: skill-name
description: "What does it do"
license: MIT
allowed-tools: Read, Grep, Glob
metadata:
  version: "1.0.0"
  domain: ai
  triggers: comma, separated, trigger, phrases
  role: expert
  scope: setup
  platforms: copilot-cli, claude
  output-format: summary
  related-skills: other-skill-1, other-skill-2
---
```

Parse with:
```powershell
$skillFile = "SKILL.md"
$content = Get-Content $skillFile -Raw

# Extract YAML between --- and ---
$yaml = $content -match '(?s)^---(.*?)---' | Out-Null
$frontmatter = $matches[1] | ConvertFrom-Yaml

$name = $frontmatter.name
$version = $frontmatter.metadata.version
$relatedSkills = $frontmatter.metadata.'related-skills' -split ','
```

## Validation During Discovery

| Check | What to Verify | Action if Missing |
|---|---|---|
| **File exists** | SKILL.md, references/*.md present | Warn, consider skipping |
| **YAML syntax** | Frontmatter is valid YAML | Warn, skip item |
| **Required fields** | name, description, version present | Warn, skip item |
| **References valid** | All mentioned .md files exist | Warn, but continue |
| **Path structure** | Follows skill/category/name/SKILL.md pattern | Warn, verify manually |
| **Readability** | Current user can read all files | Error, cannot proceed |

