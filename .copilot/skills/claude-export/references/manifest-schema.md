# Manifest Schema Reference

## Purpose

The manifest is a machine-readable inventory of all items exported to project `.claude`. It enables:
- Quick lookup of what's installed
- Version tracking and update detection
- Integrity verification via checksums
- Sync reconciliation (which items are out of date?)
- External sharing of project config

## Schema Definition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["version", "exportedAt", "sourceGlobal", "projectRoot", "items", "summary"],
  "properties": {
    "version": {
      "type": "string",
      "description": "Manifest schema version (currently 1.0.0)",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "exportedAt": {
      "type": "string",
      "format": "date-time",
      "description": "ISO 8601 timestamp when export was performed"
    },
    "exportedBy": {
      "type": "string",
      "description": "Tool/user that created this export (e.g., 'claude-export 1.0.0', 'manual')"
    },
    "sourceGlobal": {
      "type": "string",
      "description": "Absolute path to global .claude directory"
    },
    "projectRoot": {
      "type": "string",
      "description": "Absolute path to project root"
    },
    "items": {
      "type": "array",
      "description": "Array of exported items",
      "items": {
        "type": "object",
        "required": ["type", "name", "version", "path"],
        "properties": {
          "type": {
            "type": "string",
            "enum": ["skill", "bridge", "extension", "settings"],
            "description": "Item type"
          },
          "name": {
            "type": "string",
            "description": "Item name (e.g., 'claude-export', 'CLAUDE.md')"
          },
          "category": {
            "type": "string",
            "description": "Category (for skills: 'ai', 'security', 'architecture', etc.)"
          },
          "version": {
            "type": "string",
            "description": "Item version (from metadata.version in SKILL.md, or file version)",
            "pattern": "^\\d+\\.\\d+\\.\\d+$"
          },
          "description": {
            "type": "string",
            "description": "Item description (from SKILL.md)"
          },
          "path": {
            "type": "string",
            "description": "Relative path from project root to this item"
          },
          "sourceChecksum": {
            "type": "string",
            "description": "SHA256 checksum of source file(s)",
            "pattern": "^sha256:[a-f0-9]{64}$"
          },
          "projectChecksum": {
            "type": "string",
            "description": "SHA256 checksum of copied file(s)",
            "pattern": "^sha256:[a-f0-9]{64}$"
          },
          "copiedAt": {
            "type": "string",
            "format": "date-time",
            "description": "ISO 8601 timestamp when this item was copied"
          },
          "license": {
            "type": "string",
            "description": "License (from SKILL.md, e.g., 'MIT', 'Apache-2.0')"
          },
          "relatedSkills": {
            "type": "array",
            "description": "Array of related skill names",
            "items": { "type": "string" }
          },
          "referenceFiles": {
            "type": "array",
            "description": "Array of reference files (for skills)",
            "items": {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "path": { "type": "string" },
                "checksum": { "type": "string", "pattern": "^sha256:[a-f0-9]{64}$" }
              }
            }
          },
          "notes": {
            "type": "string",
            "description": "Any additional notes (e.g., 'Manual update required', 'Partial copy')"
          }
        }
      }
    },
    "summary": {
      "type": "object",
      "required": ["totalItems"],
      "properties": {
        "totalItems": {
          "type": "integer",
          "description": "Total number of items exported"
        },
        "skillsCount": {
          "type": "integer"
        },
        "bridgesCount": {
          "type": "integer"
        },
        "extensionsCount": {
          "type": "integer"
        },
        "settingsCount": {
          "type": "integer"
        },
        "checksumMismatches": {
          "type": "integer",
          "description": "Count of items with source != project checksum"
        },
        "totalSize": {
          "type": "string",
          "description": "Estimated total size of exported items (e.g., '12.3 MB')"
        },
        "exportDuration": {
          "type": "string",
          "description": "How long the export took (e.g., '2.5s')"
        }
      }
    },
    "warnings": {
      "type": "array",
      "description": "Array of warnings encountered during export",
      "items": {
        "type": "object",
        "properties": {
          "level": {
            "type": "string",
            "enum": ["info", "warning", "error"]
          },
          "item": {
            "type": "string",
            "description": "Item name or path related to warning"
          },
          "message": {
            "type": "string"
          }
        }
      }
    }
  }
}
```

## Example Manifest

```json
{
  "version": "1.0.0",
  "exportedAt": "2026-04-15T18:18:10Z",
  "exportedBy": "claude-export 1.0.0",
  "sourceGlobal": "C:\\Users\\username\\.claude",
  "projectRoot": "C:\\Projects\\EscrowApp",
  "items": [
    {
      "type": "skill",
      "name": "claude-export",
      "category": "ai",
      "version": "1.0.0",
      "description": "Export and sync global .claude configuration",
      "path": ".claude/skills/ai/claude-export/SKILL.md",
      "sourceChecksum": "sha256:a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f",
      "projectChecksum": "sha256:a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f",
      "copiedAt": "2026-04-15T18:18:10Z",
      "license": "MIT",
      "relatedSkills": ["agent-orchestrator", "mcp-developer"],
      "referenceFiles": [
        {
          "name": "discovery-pattern.md",
          "path": ".claude/skills/ai/claude-export/references/discovery-pattern.md",
          "checksum": "sha256:b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2"
        },
        {
          "name": "manifest-schema.md",
          "path": ".claude/skills/ai/claude-export/references/manifest-schema.md",
          "checksum": "sha256:c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f23"
        }
      ]
    },
    {
      "type": "bridge",
      "name": "CLAUDE.md",
      "version": "1.2.0",
      "description": "Claude-specific instruction bridge",
      "path": ".claude/CLAUDE.md",
      "sourceChecksum": "sha256:d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f234",
      "projectChecksum": "sha256:d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f234",
      "copiedAt": "2026-04-15T18:18:10Z",
      "license": "MIT"
    },
    {
      "type": "settings",
      "name": "settings.json",
      "version": "1.0.0",
      "path": ".claude/settings.json",
      "sourceChecksum": "sha256:e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2345",
      "projectChecksum": "sha256:e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2345",
      "copiedAt": "2026-04-15T18:18:10Z"
    }
  ],
  "summary": {
    "totalItems": 3,
    "skillsCount": 1,
    "bridgesCount": 1,
    "extensionsCount": 0,
    "settingsCount": 1,
    "checksumMismatches": 0,
    "totalSize": "145.2 KB",
    "exportDuration": "1.2s"
  },
  "warnings": []
}
```

## Manifest Operations

### Reading the Manifest

```powershell
$manifest = Get-Content .claude/manifest.json | ConvertFrom-Json

# Get all skills
$skills = $manifest.items | Where-Object { $_.type -eq "skill" }

# Get all bridges
$bridges = $manifest.items | Where-Object { $_.type -eq "bridge" }

# Check for version mismatches
$mismatches = $manifest.items | Where-Object { $_.sourceChecksum -ne $_.projectChecksum }
if ($mismatches) {
  Write-Host "Items out of sync with global: $($mismatches.Count)"
}

# List all related skills
$relatedTo = $manifest.items | Where-Object { $_.name -eq "claude-export" } | Select-Object -ExpandProperty relatedSkills
```

### Updating the Manifest

When syncing after adding/removing items:

```powershell
$manifest = Get-Content .claude/manifest.json | ConvertFrom-Json

# Add new item
$newItem = @{
  type = "skill"
  name = "new-skill"
  version = "1.0.0"
  path = ".claude/skills/ai/new-skill/SKILL.md"
  sourceChecksum = "sha256:..."
  projectChecksum = "sha256:..."
  copiedAt = (Get-Date -AsUTC -Format "o")
}

$manifest.items += $newItem
$manifest.summary.totalItems = $manifest.items.Count
$manifest.summary.skillsCount += 1
$manifest.exportedAt = (Get-Date -AsUTC -Format "o")

$manifest | ConvertTo-Json -Depth 10 | Set-Content .claude/manifest.json
```

## Integrity Validation

### Checksum Verification

```powershell
function Test-ManifestIntegrity {
  param($manifestPath)
  
  $manifest = Get-Content $manifestPath | ConvertFrom-Json
  $baseDir = Split-Path $manifestPath
  
  $issues = @()
  
  foreach ($item in $manifest.items) {
    $fullPath = Join-Path $baseDir $item.path
    
    if (-not (Test-Path $fullPath)) {
      $issues += "Missing: $($item.path)"
      continue
    }
    
    $actualChecksum = (Get-FileHash $fullPath -Algorithm SHA256).Hash
    $expectedChecksum = $item.projectChecksum -replace "sha256:", ""
    
    if ($actualChecksum -ne $expectedChecksum) {
      $issues += "Checksum mismatch: $($item.path)"
    }
  }
  
  return $issues
}

$issues = Test-ManifestIntegrity ".claude/manifest.json"
if ($issues) {
  Write-Host "Manifest integrity issues:"
  $issues | ForEach-Object { Write-Host "  ⚠️ $_" }
} else {
  Write-Host "✅ Manifest integrity OK"
}
```

