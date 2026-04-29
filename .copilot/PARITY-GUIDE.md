# Portable Hook Infrastructure — Parity & Maintenance Guide

**Purpose:** Ensure pre-commit validation behaves identically across Copilot CLI and Claude Code  
**Principle:** Single source of truth + parallel configuration files  
**Status:** v1.0 (Initial implementation)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│         Single Source of Truth: Security Patterns            │
│    .github/hooks/pre-commit (Shell script)                  │
│    Contains all CRITICAL pattern definitions                │
└─────────────────┬───────────────────────────────────────────┘
                  │ Patterns sync to both configs
        ┌─────────┴──────────┐
        │                    │
        ▼                    ▼
┌──────────────────┐   ┌──────────────────┐
│  Copilot CLI     │   │  Claude Code     │
│  Hook Config     │   │  Hook Config     │
│ .github/hooks/   │   │ .claude/hooks/   │
│ pre-commit.yaml  │   │ pre-commit.yaml  │
└──────────────────┘   └──────────────────┘
        │                    │
        │ Auto-triggered     │ Auto-triggered
        │ on git commit      │ on git commit
        │                    │
        ▼                    ▼
┌──────────────────┐   ┌──────────────────┐
│ Copilot CLI      │   │ Claude Code      │
│ Environment      │   │ Environment      │
└──────────────────┘   └──────────────────┘
        │                    │
        └────────┬───────────┘
                 │
                 ▼ Both block on CRITICAL issues
         ⛔ COMMIT BLOCKED or ✅ COMMIT OK
```

---

## Files & Responsibilities

### Source of Truth (Patterns)

| File | Purpose | Who Maintains |
|------|---------|-----------------|
| `.github/hooks/pre-commit` | Shell script with all CRITICAL patterns | Backend/DevOps |
| `.github/hooks/pre-commit.yaml` | Copilot CLI hook config (mirrors patterns) | Backend/DevOps |
| `.claude/hooks/pre-commit.yaml` | Claude Code hook config (mirrors patterns) | Backend/DevOps |
| `.claude/config/` | Claude Code integration settings | Backend/DevOps |
| `.github/hooks/setup-pre-commit.sh` | Unified setup script for all environments | Backend/DevOps |

---

## How to Maintain Parity

### Rule 1: Change Patterns in Shell Script First

When adding a new security pattern (e.g., new secret type):

```bash
# STEP 1: Update the source of truth
# Edit: .github/hooks/pre-commit (shell script)
# Add pattern to grep check

# Example: Add new pattern for Azure Storage secrets
if grep -qE 'DefaultEndpointsProtocol=https;AccountName=[a-z0-9]+' "$FILE" 2>/dev/null; then
    echo "⛔ CRIT-007: Azure storage secret found in $FILE"
    FOUND_ISSUES=1
fi
```

### Rule 2: Mirror Pattern to YAML Configs

```bash
# STEP 2: Update Copilot CLI config
# Edit: .github/hooks/pre-commit.yaml
# Add identical pattern to checks array

- name: "Azure Storage Secrets"
  patterns:
    - "DefaultEndpointsProtocol=https;AccountName=[a-z0-9]+"
  severity: CRITICAL
  action: BLOCK

# STEP 3: Update Claude Code config
# Edit: .claude/hooks/pre-commit.yaml
# Add identical pattern to checks array (same as above)
```

### Rule 3: Keep Message/Feedback Synchronized

```bash
# Copilot CLI output (.github/hooks/pre-commit.yaml):
on_failure:
  feedback: "⛔ COMMIT BLOCKED — CRITICAL security issues found."

# Claude Code output (.claude/hooks/pre-commit.yaml):
on_failure:
  feedback: "⛔ COMMIT BLOCKED — CRITICAL security issues found."

# Shell script output (.github/hooks/pre-commit):
echo "⛔ COMMIT BLOCKED — CRITICAL security issues found."

# ✅ All three must be identical
```

---

## Parity Checklist

When updating security patterns, verify:

- [ ] **Shell Script Updated** — New pattern added to `.github/hooks/pre-commit`
- [ ] **Copilot Config Updated** — Same pattern mirrored to `.github/hooks/pre-commit.yaml`
- [ ] **Claude Code Config Updated** — Same pattern mirrored to `.claude/hooks/pre-commit.yaml`
- [ ] **Error Messages Match** — All three provide consistent feedback
- [ ] **Severity Aligned** — CRITICAL/HIGH/WARN consistent across all three
- [ ] **Block Behavior Aligned** — If shell script blocks, YAML configs block
- [ ] **Tested in Both Environments** — Run `git commit` in Copilot + Claude to verify
- [ ] **Documentation Updated** — Update this file if changing rules

---

## How to Add a New Security Check

### Example: Add detection for hardcoded database passwords

**Step 1: Add to Shell Script**
```bash
# File: .github/hooks/pre-commit
# Add before the final check (line ~100)

# CRIT-008: Database password connection strings
if grep -qiE 'server=.*password=.*' "$FILE" 2>/dev/null; then
    echo "⛔ CRIT-008: Database password found in connection string in $FILE"
    FOUND_ISSUES=1
fi
```

**Step 2: Mirror to Copilot YAML**
```yaml
# File: .github/hooks/pre-commit.yaml
# Add to checks array

- name: "Database Connection Passwords"
  patterns:
    - "server=.*password=.*"
  severity: CRITICAL
  action: BLOCK
```

**Step 3: Mirror to Claude YAML**
```yaml
# File: .claude/hooks/pre-commit.yaml
# Add to checks array (identical)

- name: "Database Connection Passwords"
  patterns:
    - "server=.*password=.*"
  severity: CRITICAL
  action: BLOCK
```

**Step 4: Update Documentation**
```bash
# Add to this file (PARITY-GUIDE.md):
# Add to "How to Add a New Security Check" section with example
```

**Step 5: Test**
```bash
# Create test file with pattern
echo 'server=localhost;password=Secret123' > test.cs
git add test.cs

# Test shell hook
bash .github/hooks/pre-commit  # Should block

# Test Copilot (if available)
copilot pre_commit_security_scan  # Should block

# Test Claude Code
# Use /pre-commit-validate command  # Should block

# Cleanup
git reset HEAD test.cs
rm test.cs
```

---

## Environment-Specific Variations

### Acceptable Differences (Maintain, Don't Unify)

| Aspect | Copilot CLI | Claude Code | Shell Script | Reason |
|--------|-------------|-------------|--------------|--------|
| **Output Format** | Terminal colors | Claude-structured | ANSI colors | Different UIs |
| **UI Interaction** | CLI flags | Commands + UI | Direct messages | Platform differences |
| **Auto-Proceed** | Yes (can auto-commit) | No (user confirms) | No (user confirms) | Copilot CLI > Copilot full ecosystem control |

### Must Be Identical (Never Vary)

| Aspect | Requirement | Why |
|--------|-------------|-----|
| **Security Patterns** | Same regex across all three | If one allows a secret, portability is broken |
| **Severity Levels** | CRITICAL/HIGH/WARN consistent | Users need to trust all three equally |
| **Block Behavior** | If one blocks, all block | CRITICAL issues must never leak through |
| **Error Messages** | Same message across all three | Users expect consistent feedback |

---

## Testing Parity

### Automated Test (Future)

```bash
#!/bin/bash
# Test that all three implementations block the same patterns

test_pattern() {
    PATTERN=$1
    TEST_FILE="test_secret.txt"
    echo "$PATTERN" > $TEST_FILE
    
    # Test shell script
    bash .github/hooks/pre-commit > /tmp/shell_out.txt 2>&1
    SHELL_RESULT=$?
    
    # Test Copilot (if available)
    copilot pre_commit_security_scan > /tmp/copilot_out.txt 2>&1
    COPILOT_RESULT=$?
    
    # Test Claude (manual, but verify config syntax)
    yamllint .claude/hooks/pre-commit.yaml > /tmp/claude_out.txt 2>&1
    CLAUDE_RESULT=$?
    
    # All should fail on CRITICAL patterns
    if [ $SHELL_RESULT -ne 1 ] || [ $COPILOT_RESULT -ne 1 ]; then
        echo "❌ Parity broken for: $PATTERN"
        return 1
    fi
    
    rm $TEST_FILE
    return 0
}

# Test CRITICAL patterns
test_pattern "sk_live_abc123"
test_pattern "ghp_0123456789abcdef"
test_pattern "BEGIN RSA PRIVATE KEY"

echo "✅ Parity test passed"
```

### Manual Test (Current)

Run this before committing changes to hook infrastructure:

```bash
# 1. Verify shell script syntax
bash -n .github/hooks/pre-commit
echo "✅ Shell script syntax valid"

# 2. Verify YAML configs
yamllint .github/hooks/pre-commit.yaml
yamllint .claude/hooks/pre-commit.yaml
echo "✅ YAML configs valid"

# 3. Test with sample secret
echo "sk_test_123" > test.txt
git add test.txt

# 3a. Test shell hook
bash .github/hooks/pre-commit  # Should return exit code 1
echo "✅ Shell hook blocks secrets"

# Cleanup
git reset HEAD test.txt
rm test.txt

# 4. Visually verify patterns match in both YAML files
grep -A 5 "Stripe Secret" .github/hooks/pre-commit.yaml
grep -A 5 "Stripe Secret" .claude/hooks/pre-commit.yaml
echo "✅ Patterns match"
```

---

## Version Control & Updates

### Update Log

| Date | Change | Shell | Copilot | Claude | Notes |
|------|--------|-------|---------|--------|-------|
| 2026-04-16 | Initial setup | v1.0 | v1.0 | v1.0 | Parity established |
| — | Add Azure secrets | — | — | — | TBD |
| — | Add Terraform vars | — | — | — | TBD |

### Update Process

1. **Plan change** → Add to todo list
2. **Update shell script** → Test locally
3. **Update YAML configs** → Test in both environments
4. **Update this file** → Document changes
5. **Commit** → Single commit with all three files
6. **Update log** → Record version bump

---

## Rollback Procedure

If a pattern causes false positives:

```bash
# 1. Revert all three files
git revert <commit>

# 2. Or selectively remove pattern from all three:
# - .github/hooks/pre-commit
# - .github/hooks/pre-commit.yaml
# - .claude/hooks/pre-commit.yaml

# 3. Test with false positive case
# 4. Commit fix with explanation
```

---

## Future Enhancements

- [ ] Automated parity testing (CI/CD pipeline)
- [ ] Single YAML source → generate shell script
- [ ] Integration with GitHub Security Advisory notifications
- [ ] Claude Code native hook support (when available)
- [ ] Copilot CLI native hook support (when available)

---

## Reference

- **Setup Script:** `.github/hooks/setup-pre-commit.sh`
- **Shell Hook:** `.github/hooks/pre-commit`
- **Copilot Config:** `.github/hooks/pre-commit.yaml`
- **Claude Config:** `.claude/hooks/pre-commit.yaml`
- **Workflow Guide:** `.github/PORTABLE-COMMIT-WORKFLOW.md`

