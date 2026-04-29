#!/bin/bash
# Session Start Hook — NexTruzt.io EscrowApp
# Outputs the meta-skill to prime any AI agent at session start.
# Compatible with: Claude Code hooks, Unix-based CI/CD

META_SKILL=".github/skills/workflow/using-skills/SKILL.md"
CATALOG=".github/skills/CATALOG.md"

if [ -f  "$META_SKILL" ]; then
    echo "=== AI Skills Meta-Skill ==="
    head -60 "$META_SKILL"
    echo ""
    echo "Full skill catalog: $CATALOG"
    echo "50 skills across 12 categories — run 'cat $CATALOG' for the full index."
else
    echo "Meta-skill not found at $META_SKILL — skills infrastructure may not be initialized."
    echo "Run 'cat $CATALOG' to browse available skills."
fi
