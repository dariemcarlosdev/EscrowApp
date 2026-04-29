# NexTruzt.io — Portable Hook Infrastructure Overview

**Infrastructure Status:** ✅ READY FOR DEPLOYMENT  
**Version:** 1.0  
**Platforms:** Windows, macOS, Linux  
**AI Assistants:** Copilot CLI ✅, Claude Code ✅, Manual ✅

---

## What Is This?

A **unified, automatically-triggered pre-commit validation system** that works identically across:
- ✅ **Copilot CLI** — Automatic validation on `git commit`
- ✅ **Claude Code** — Validation via command or auto-offer
- ✅ **Manual/Git** — Git hook backup for all environments

**Key Principle:** Whatever is implemented for Copilot must be **paralleled and mirrored for Claude Code**, maintaining **portability** and **parity**.

---

## Quick Install

```bash
# One-time setup (all platforms)
bash .github/hooks/setup-pre-commit.sh

# That's it! Hooks are now active.
```

---

## How It Works

### Layer 1: Git Pre-Commit Hook (Always Active)

```
User types: git commit
    ↓
Git automatically runs: .git/hooks/pre-commit
    ↓
Shell script checks for:
  ✓ Stripe secrets (sk_live_*, sk_test_*)
  ✓ GitHub tokens (ghp_*, gho_*, etc.)
  ✓ AWS keys (AKIA*, aws_secret_access_key)
  ✓ Hardcoded passwords
  ✓ Private keys
  ✓ NexSynapse IP files (AGENTS.md, etc.)
    ↓
⛔ BLOCKED or ✅ COMMIT OK
```

### Layer 2: Copilot CLI Integration (Auto-Triggered)

```
User types: git commit
    ↓
Copilot CLI intercepts (if configured)
    ↓
Reads: .github/hooks/pre-commit.yaml
    ↓
Runs validation checks from config
    ↓
⛔ BLOCKED or ✅ COMMIT OK
```

### Layer 3: Claude Code Integration (Auto-Offered)

```
User attempts commit in Claude editor
    ↓
Claude Code detects intent
    ↓
Reads: .claude/hooks/pre-commit.yaml
    ↓
Offers to validate (or auto-validates)
    ↓
Shows results in Claude UI
    ↓
User confirms: ✅ Proceed or ❌ Fix issues
```

---

## File Structure

```
.github/
├── hooks/
│   ├── pre-commit                    ← Shell script (source of truth)
│   ├── pre-commit.yaml               ← Copilot CLI config
│   ├── setup-pre-commit.sh           ← Setup automation
│   └── SETUP-AND-VERIFY.md           ← Install guide
├── PORTABLE-COMMIT-WORKFLOW.md       ← User workflow guide
├── PARITY-GUIDE.md                   ← Maintainer parity rules
└── HOOK-INFRASTRUCTURE-SUMMARY.md    ← This file

.claude/
├── hooks/
│   └── pre-commit.yaml               ← Claude Code config
├── config/
└── settings.json                     ← Claude Code integration

.git/
└── hooks/
    └── pre-commit                    ← Actual Git hook (installed by setup)
```

---

## Key Files Explained

### Source of Truth: `.github/hooks/pre-commit`

```bash
# Shell script that defines all security patterns
# This is where new patterns are FIRST added

if grep -qE 'sk_(live|test)_[a-zA-Z0-9]{20,}' "$FILE"; then
    echo "⛔ CRIT-001: Stripe secret key found in $FILE"
fi
```

**Who updates it?** Backend/DevOps team  
**When?** When adding new security checks  
**Rule:** Must be mirrored to both YAML configs immediately

---

### Copilot Config: `.github/hooks/pre-commit.yaml`

```yaml
# Copilot CLI configuration
# Mirrors patterns from shell script

trigger: git-commit
environment: copilot-cli

checks:
  - name: "Stripe Secret Keys"
    patterns:
      - "sk_(live|test)_[a-zA-Z0-9]{20,}"
    severity: CRITICAL
    action: BLOCK
```

**Who updates it?** Backend/DevOps team  
**When?** When shell script is updated  
**Rule:** Patterns must be identical to shell script

---

### Claude Config: `.claude/hooks/pre-commit.yaml`

```yaml
# Claude Code configuration
# Mirrors patterns from shell script

trigger: git-commit
environment: claude-code

checks:
  - name: "Stripe Secret Keys"
    patterns:
      - "sk_(live|test)_[a-zA-Z0-9]{20,}"
    severity: CRITICAL
    action: BLOCK
```

**Who updates it?** Backend/DevOps team  
**When?** When shell script is updated  
**Rule:** Patterns must be identical to shell script

---

### Setup Script: `.github/hooks/setup-pre-commit.sh`

```bash
#!/bin/bash
# Automated setup that:
# 1. Installs Git hook
# 2. Configures Copilot CLI
# 3. Configures Claude Code
# 4. Verifies everything is working
```

**Run:** `bash .github/hooks/setup-pre-commit.sh`  
**Does:** Installs hooks for all environments  
**Result:** Hooks active immediately

---

## Parity Principle

### What Is Parity?

**Parity** = Same security behavior across all platforms

✅ If shell script blocks a pattern, both YAML configs block the same pattern  
✅ If Copilot CLI blocks, Claude Code blocks with same severity  
✅ All three show consistent error messages

### How Parity Is Maintained

**Rule:** When adding a new security pattern:

```bash
# 1. Update shell script first (source of truth)
vim .github/hooks/pre-commit
# Add: if grep -qE 'pattern' "$FILE"; then echo "⛔ message"; fi

# 2. Update Copilot config
vim .github/hooks/pre-commit.yaml
# Add:   - name: "Check Name"
#         patterns: ["pattern"]

# 3. Update Claude config
vim .claude/hooks/pre-commit.yaml
# Add:   - name: "Check Name"
#         patterns: ["pattern"]

# 4. Test in both environments
bash .github/hooks/pre-commit  # Test shell
copilot pre_commit_security_scan  # Test Copilot (if available)
# Test Claude Code: /pre-commit-validate command

# 5. Single atomic commit
git add .github/hooks/* .claude/hooks/*
git commit -m "security: add [pattern] check"
```

**See `.github/PARITY-GUIDE.md` for detailed parity maintenance procedures**

---

## Usage Examples

### Example 1: New User (Any Platform)

```bash
# Step 1: One-time setup
bash .github/hooks/setup-pre-commit.sh
# ✅ Git pre-commit hook installed
# ✅ Copilot CLI hook config present
# ✅ Claude Code hook config present
# ✅ Setup complete!

# Step 2: Normal development (no changes needed)
# Code, test, commit as usual

# Step 3: Hooks run automatically
git commit -m "feat(auth): implement login"
# 🛡️  NexSynapse Pre-Commit Security Guard
# ✅ Pre-commit validation passed. Proceeding with commit.
```

### Example 2: Copilot CLI User

```bash
# After setup, Copilot will auto-validate on commit
$ git commit -m "feat(auth): implement login"

# Copilot CLI detects commit attempt
# → Reads .github/hooks/pre-commit.yaml
# → Runs security checks
# → Shows feedback in terminal
# → Commits if clean, blocks if issues

# To skip (not recommended):
$ git commit --no-verify
```

### Example 3: Claude Code User

```bash
# In Claude Code, before committing:
# Type: /pre-commit-validate

# Claude reads .claude/hooks/pre-commit.yaml
# → Runs validation
# → Shows results in Claude UI
# → Highlights issues
# → User fixes and retries

# Or, when attempting commit:
# Claude detects intent
# → Offers validation
# → Shows results
# → User proceeds or fixes
```

### Example 4: Backend Developer Adding New Pattern

```bash
# Goal: Block hardcoded database passwords

# Step 1: Update shell script (source of truth)
vim .github/hooks/pre-commit
# Add after line 87:
if grep -qiE 'password=.*=' "$FILE" 2>/dev/null; then
    echo "⛔ CRIT-009: Database password found in $FILE"
    FOUND_ISSUES=1
fi

# Step 2: Update Copilot config
vim .github/hooks/pre-commit.yaml
# Add to checks array:
- name: "Database Passwords"
  patterns:
    - "password=.*="
  severity: CRITICAL
  action: BLOCK

# Step 3: Update Claude config
vim .claude/hooks/pre-commit.yaml
# Add to checks array (identical):
- name: "Database Passwords"
  patterns:
    - "password=.*="
  severity: CRITICAL
  action: BLOCK

# Step 4: Test
echo 'password=secret123' > test.cs
git add test.cs
bash .github/hooks/pre-commit  # Should block
git reset HEAD test.cs
rm test.cs

# Step 5: Commit all changes
git add .github/hooks/* .claude/hooks/*
git commit -m "security: add database password check"
```

---

## Maintenance

### Checklist When Updating Patterns

- [ ] Update `.github/hooks/pre-commit` (shell script)
- [ ] Update `.github/hooks/pre-commit.yaml` (Copilot config)
- [ ] Update `.claude/hooks/pre-commit.yaml` (Claude config)
- [ ] Error messages identical across all three
- [ ] Severity levels aligned (CRITICAL/HIGH/WARN)
- [ ] Block behavior synchronized
- [ ] Test in shell: `bash .github/hooks/pre-commit`
- [ ] Test in Copilot (if available): `copilot pre_commit_security_scan`
- [ ] Test in Claude Code: `/pre-commit-validate` command
- [ ] Update `.github/PARITY-GUIDE.md` version log
- [ ] Single commit: `git commit -m "security: add X check"`

**See `.github/PARITY-GUIDE.md` for complete maintenance guide**

---

## Testing

### Quick Test (All Users)

```bash
# Test 1: Verify hook blocks secrets
echo 'sk_live_abc123' > test.txt
git add test.txt
git commit -m "test"  # Should fail

# Test 2: Verify hook allows clean code
echo 'public class Login { }' > Login.cs
git add Login.cs
git commit -m "feat: add login"  # Should succeed
git reset --soft HEAD~1
rm Login.cs test.txt
```

**See `.github/hooks/SETUP-AND-VERIFY.md` for detailed testing guide**

---

## Documentation

| Document | Purpose | Audience |
|----------|---------|----------|
| `.github/PORTABLE-COMMIT-WORKFLOW.md` | How to use the workflow | All users |
| `.github/PARITY-GUIDE.md` | How to maintain parity | Backend/DevOps |
| `.github/hooks/SETUP-AND-VERIFY.md` | Installation & testing | Users setting up |
| `.github/HOOK-INFRASTRUCTURE-SUMMARY.md` | Overview | Everyone |

---

## Architecture Benefits

✅ **Single Source of Truth**
- Patterns defined once in shell script
- Mirrored to both YAML configs
- No duplication of logic

✅ **True Portability**
- Same behavior across Copilot CLI, Claude Code, manual Git
- Works on Windows, macOS, Linux
- Zero external dependencies

✅ **Automatic Enforcement**
- No manual steps needed after setup
- Hooks run automatically on every commit
- Blocks CRITICAL issues immediately

✅ **Maintainable**
- Clear parity rules in `.github/PARITY-GUIDE.md`
- Automated setup script
- Easy to add new patterns

✅ **Fintech-Ready**
- Blocks secrets (Stripe, AWS, GitHub tokens)
- Blocks IP files (NexSynapse)
- Blocks injection patterns
- Audit trail via Git history

---

## Troubleshooting

### Hook Not Running?
```bash
ls -la .git/hooks/pre-commit
# If missing, reinstall: bash .github/hooks/setup-pre-commit.sh
```

### Permission Issues?
```bash
chmod +x .git/hooks/pre-commit  # macOS/Linux
# Windows (Git Bash): should work as-is
```

### Pattern Too Broad?
See `.github/PARITY-GUIDE.md` → Rollback Procedure

**Full troubleshooting guide:** `.github/hooks/SETUP-AND-VERIFY.md`

---

## Next Steps

1. **Install:** `bash .github/hooks/setup-pre-commit.sh`
2. **Test:** Follow `.github/hooks/SETUP-AND-VERIFY.md`
3. **Learn:** Read `.github/PORTABLE-COMMIT-WORKFLOW.md`
4. **Maintain:** Read `.github/PARITY-GUIDE.md` (for maintainers)

---

## Summary

✅ **Unified** → Same validation across Copilot + Claude  
✅ **Automatic** → Hooks run on every git commit  
✅ **Portable** → Works everywhere (Windows, macOS, Linux)  
✅ **Synchronized** → Patterns mirrored across all platforms  
✅ **Maintainable** → Clear parity rules, single source of truth  

**Status: Ready for deployment.** Run setup and hooks are active immediately.

