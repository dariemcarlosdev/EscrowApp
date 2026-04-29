#!/usr/bin/env pwsh
# Session Start Hook — NexTruzt.io EscrowApp
# Outputs the meta-skill to prime any AI agent at session start.
# Compatible with: Copilot CLI, Claude Code, Codex CLI

$metaSkill = ".github/skills/workflow/using-skills/SKILL.md"
$catalog = ".github/skills/CATALOG.md"

if (Test-Path $metaSkill) {
    Write-Host "=== AI Skills Meta-Skill ==="
    Get-Content $metaSkill | Select-Object -First 60
    Write-Host ""
    Write-Host "Full skill catalog: $catalog"
    Write-Host "50 skills across 12 categories — run 'cat $catalog' for the full index."
} else {
    Write-Host "Meta-skill not found at $metaSkill — skills infrastructure may not be initialized."
    Write-Host "Run 'cat .github/skills/CATALOG.md' to browse available skills."
}
