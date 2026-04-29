# Hook Infrastructure Setup & Verification

**Status:** Ready for installation  
**Platforms:** Windows (Git Bash), macOS, Linux  
**Environments:** Copilot CLI, Claude Code, Manual

---

## Quick Install (One Command)

```bash
# Linux/macOS/Git Bash on Windows
bash .github/hooks/setup-pre-commit.sh
```

**Output should show:**
```
🛡️  NexSynapse Portable Pre-Commit Hook Setup
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Git pre-commit hook installed
✅ Copilot CLI hook config present
✅ Claude Code hook config present
✅ Claude Code settings configured
✅ Setup complete! Pre-commit hooks are ready.
```

---

## Manual Install (Step by Step)

### Step 1: Install Git Pre-Commit Hook

```bash
# Copy hook to .git/hooks
cp .github/hooks/pre-commit .git/hooks/pre-commit

# Make executable (macOS/Linux)
chmod +x .git/hooks/pre-commit

# On Windows, this should work as-is in Git Bash
```

### Step 2: Verify Installation

```bash
# Check hook exists and is executable
ls -la .git/hooks/pre-commit

# Expected output:
# -rwxr-xr-x  1 user  group  5234 Apr 16 02:00 .git/hooks/pre-commit
```

### Step 3: Configure Copilot CLI (If Using)

```bash
# Check if Copilot CLI is available
copilot --version

# Configure pre-commit hook support (when available)
copilot config set pre-commit.enabled=true
```

### Step 4: Configure Claude Code (If Using)

The setup script already created `.claude/settings.json`. No additional steps needed.

To enable the command in Claude:
1. Open Claude Code
2. Type `/` to see available commands
3. Look for `pre-commit-validate`
4. Use before committing to run validation

---

## Verification Checklist

After setup, verify all components:

```bash
# ✅ Git hook installed
[ -x .git/hooks/pre-commit ] && echo "✅ Git hook installed" || echo "❌ Git hook missing"

# ✅ Copilot config present
[ -f .github/hooks/pre-commit.yaml ] && echo "✅ Copilot config present" || echo "❌ Copilot config missing"

# ✅ Claude config present
[ -f .claude/hooks/pre-commit.yaml ] && echo "✅ Claude config present" || echo "❌ Claude config missing"

# ✅ Claude settings configured
[ -f .claude/settings.json ] && echo "✅ Claude settings present" || echo "❌ Claude settings missing"
```

---

## How to Test the Hooks

### Test 1: Verify Hook Blocks Secrets

```bash
# Create a test file with a secret
echo 'sk_live_abc123xyz' > test_secret.cs
git add test_secret.cs

# Try to commit (should fail)
git commit -m "test: add secret"

# Expected output:
# 🛡️  NexSynapse Pre-Commit Security Guard
# ⛔ CRIT-001: Stripe secret key found in test_secret.cs
# ⛔ COMMIT BLOCKED — CRITICAL security issues found.

# Clean up
git reset HEAD test_secret.cs
rm test_secret.cs
```

### Test 2: Verify Hook Allows Clean Commits

```bash
# Create a legitimate file
echo 'public class Login { }' > Login.cs
git add Login.cs

# Commit (should succeed)
git commit -m "feat: add login class"

# Expected output:
# 🛡️  NexSynapse Pre-Commit Security Guard
# ✅ Pre-commit security scan passed. Proceeding with commit.

# Reset for testing
git reset --soft HEAD~1
rm Login.cs
```

### Test 3: Test Copilot CLI Integration

```bash
# If Copilot CLI is installed
copilot pre_commit_security_scan

# Should display security report
```

### Test 4: Test Claude Code Integration

In Claude Code:
1. Type `/pre-commit-validate`
2. Should offer to run pre-commit validation
3. Displays results in Claude's interface

---

## Troubleshooting

### Problem: Hook Not Running

```bash
# Check if hook exists
ls -la .git/hooks/pre-commit

# If missing, reinstall
cp .github/hooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### Problem: Permission Denied

**Windows (Git Bash):**
```bash
chmod +x .git/hooks/pre-commit
```

**macOS/Linux:**
```bash
chmod 755 .git/hooks/pre-commit
```

### Problem: Hook Blocks Legitimate Code

Check if pattern is too broad:
1. Note which file was blocked
2. Review pattern in `.github/hooks/pre-commit`
3. Adjust regex if needed
4. Update both YAML configs to match
5. See `.github/PARITY-GUIDE.md` for update procedure

### Problem: Claude Code Not Offering Validation

```bash
# Verify settings file exists
cat .claude/settings.json

# If missing or corrupt, reinstall
bash .github/hooks/setup-pre-commit.sh
```

---

## Bypass (Emergency Only)

**⚠️ WARNING: Only use if you're absolutely certain no secrets are being committed**

```bash
# Skip pre-commit hook for ONE commit
git commit --no-verify -m "emergency: bypass hook"

# This is NOT recommended for fintech/security code
```

---

## Architecture Files

| File | Purpose |
|------|---------|
| `.git/hooks/pre-commit` | Actual Git hook (shell script) |
| `.github/hooks/pre-commit` | Source of truth for Git hook |
| `.github/hooks/pre-commit.yaml` | Copilot CLI configuration |
| `.github/hooks/setup-pre-commit.sh` | Setup automation script |
| `.claude/hooks/pre-commit.yaml` | Claude Code configuration |
| `.claude/config/` | Claude Code integration settings |
| `.claude/settings.json` | Claude Code command registration |
| `.github/PORTABLE-COMMIT-WORKFLOW.md` | User-facing workflow guide |
| `.github/PARITY-GUIDE.md` | Maintainer parity documentation |

---

## Integration with IDEs

### VS Code (with GitLens)

The hook will run automatically. If it blocks a commit:
1. GitLens shows error message
2. Review the blocked files in the terminal
3. Fix the issue (remove secret, etc.)
4. Try committing again

### JetBrains IDEs (Rider, IntelliJ)

The hook will run in the commit dialog. If it blocks:
1. Error message appears in commit dialog
2. Click "View Details" to see blocked files
3. Cancel commit, fix issue
4. Retry commit

### Claude Code

Use the `/pre-commit-validate` command:
1. Before committing, type `/pre-commit-validate` in Claude
2. Claude runs the validation and shows results
3. If issues found, fix and rerun validation
4. Proceed with `git commit` once validated

### Copilot CLI

Hooks run automatically with `git commit`:
1. Terminal shows validation output
2. If blocked, fix issues
3. Retry commit

---

## Next Steps

1. **Run setup:** `bash .github/hooks/setup-pre-commit.sh`
2. **Test:** Follow "How to Test the Hooks" section above
3. **Verify:** Run checklist to confirm all components
4. **Commit:** Ready to use!

For detailed workflow guide, see: `.github/PORTABLE-COMMIT-WORKFLOW.md`  
For maintenance guide, see: `.github/PARITY-GUIDE.md`

