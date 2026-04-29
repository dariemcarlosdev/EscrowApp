# Portable Pre-Commit Validation Workflow

**For:** Claude Code, Copilot CLI, Gemini, and Manual Workflows  
**Purpose:** Automatically validate code quality, security, and compliance before committing  
**Portable:** Works cross-platform (Windows, macOS, Linux)  
**Infrastructure:** Hooks in `.github/hooks/` and `.claude/hooks/` with parity across all platforms

---

## 🚀 Quick Start — One-Time Setup

### Initialize Pre-Commit Hooks

```bash
# Run setup script once (Linux/macOS/Git Bash on Windows)
bash .github/hooks/setup-pre-commit.sh

# Output:
# 🛡️  NexSynapse Portable Pre-Commit Hook Setup
# ✅ Git pre-commit hook installed
# ✅ Copilot CLI hook config present
# ✅ Claude Code hook config present
# ✅ Claude Code settings configured
# ✅ Setup complete! Pre-commit hooks are ready.
```

After setup, **hooks run automatically** on every `git commit`.

---

## Three-Layer Security Protection (Auto-Triggered)

This project has **three independent layers** that protect against committing secrets and vulnerabilities:

| Layer | How It's Triggered | What It Catches | Environment |
|-------|-------------------|---|---|
| **Git Pre-Commit Hook** | `git commit` (automatic) | CRITICAL: secrets, IP files | CLI, Manual, All |
| **Claude Code Hook** | `git commit` or `/pre-commit-validate` | CRITICAL + HIGH (full scan) | Claude Code |
| **Copilot CLI Hook** | `git commit` or `copilot pre_commit_security_scan` | CRITICAL + HIGH (full scan) | Copilot CLI |

All three **sync with the same security patterns** (parity maintained).

---

## Full Workflow (Recommended)

### Phase 1: Development (Before Staging)

```bash
# Write code, create tests, implement features
# (use TDD: red-green-refactor cycle)
```

### Phase 2: Pre-Commit Validation (Before git add)

#### 2a. Build & Test
```bash
# Ensure code compiles and tests pass
dotnet build
dotnet test
```

**Expected:**
- ✅ Zero build warnings
- ✅ All tests passing (71+ tests)
- ✅ No broken imports

#### 2b. Security Audit (Choose One Path)

**Path A: Using AI Assistant (Claude Code, Copilot, Gemini)**

Read the security skill and follow the core workflow:
```bash
cat .github/skills/security/pre-commit-guard/SKILL.md
```

Then manually scan staged files:
```bash
# Scan for hardcoded secrets
grep -r "sk_live_\|sk_test_\|ghp_\|gho_\|akia" EscrowApp/ EscrowApp.Tests/ --include="*.cs" --include="*.json" --include="*.config"

# Scan for SQL injection patterns
grep -r "FromSqlRaw\|ExecuteSqlRaw" EscrowApp/ --include="*.cs"

# Scan for hardcoded passwords
grep -r "password.*=\|pwd.*=" EscrowApp/ --include="*.cs" --include="*.json" | grep -v "//"
```

**Path B: Using Copilot CLI (if available)**

```bash
# Built-in security scan tool
pre_commit_security_scan
```

**Path C: Manual Full OWASP Audit**

```bash
# Read the comprehensive OWASP skill
cat .github/skills/security/owasp-audit/SKILL.md

# Then check the codebase against:
# A01: Broken Access Control → [Authorize] on endpoints
# A02: Cryptographic Failures → No secrets in code
# A03: Injection → No raw SQL concatenation
# A05: Misc Config → HTTPS, headers configured
# A07: Auth Failures → Password hashing, lockout
# A09: Logging → No PII in logs
```

**Expected Results:**
- ✅ 0 CRITICAL issues
- ✅ All HIGH issues documented in task
- ✅ MEDIUM issues logged but can defer

#### 2c. Code Quality Check (Optional but Recommended)

```bash
# Check code conventions
check_conventions "EscrowApp"

# Review test coverage
dotnet test /p:CollectCoverage=true
```

### Phase 3: Stage & Commit

#### 3a. Git Add
```bash
# Stage all changes
git add .

# Or stage specific files
git add EscrowApp/Features/Auth/Login/*
git add EscrowApp.Tests/Features/Auth/Login/*
```

#### 3b. Pre-Commit Hook Runs Automatically

```
🛡️  NexSynapse Pre-Commit Security Guard
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Pre-commit security scan passed. Proceeding with commit.
```

**If hook fails:**
```
⛔ BLOCKED — NexSynapse IP protection triggered!
   These files contain proprietary AI infrastructure and MUST NOT be committed:
   🔒 AGENTS.md
   Unstage with: git reset HEAD <file>
```

**To bypass (DANGEROUS):**
```bash
# Only if you're 100% sure there are no secrets
git commit --no-verify
```

#### 3c. Commit with Conventional Message

```bash
git commit -m "feat(auth): implement login page and handler

- Create Login.razor|.cs|.css component (Slice 5)
- Add LoginCommand and LoginCommandHandler
- Integrate SignInManager for authentication
- Add unit tests (5 tests)
- Update SharedResource.resx with UI strings"
```

**Conventional Commit Format:**
```
<type>(<scope>): <subject>

<body>

Co-authored-by: [Name] <[Email]>
```

**Types:** `feat`, `fix`, `docs`, `test`, `chore`, `refactor`, `perf`, `security`  
**Scopes:** `auth`, `escrow`, `payments`, `ui`, `db`, `api`, `infra`

---

## Environment-Specific Workflows

### Copilot CLI (GitHub Copilot in Terminal)

```bash
# 1. Run the built-in pre-commit security scan
copilot security-audit

# 2. Manually stage and commit
git add .
git commit -m "feat(auth): ..."

# 3. The pre-commit hook runs automatically
```

### Claude Code (Claude's Code Editor)

```bash
# 1. Read the security skill
cat .github/skills/security/pre-commit-guard/SKILL.md

# 2. Follow the Core Workflow (5 steps in the skill file)

# 3. In terminal, run:
dotnet build && dotnet test
git add .
git commit -m "feat(auth): ..."
```

### Gemini (Gemini in IDE)

```bash
# 1. Read security skill
cat .github/skills/security/pre-commit-guard/SKILL.md

# 2. Scan staged files per skill guidance

# 3. In terminal:
git add .
git commit -m "feat(auth): ..."
```

### Manual/No AI Assistant

```bash
# Full manual pre-commit validation

# Step 1: Build & Test
dotnet build || exit 1
dotnet test || exit 1

# Step 2: Scan for secrets (manual)
if grep -r "sk_live_\|sk_test_\|ghp_\|gho_" EscrowApp/ EscrowApp.Tests/; then
  echo "❌ CRITICAL: Secrets detected. Fix before committing."
  exit 1
fi

# Step 3: Scan for SQL injection (manual)
if grep -r "FromSqlRaw\|ExecuteSqlRaw" EscrowApp/; then
  echo "⚠️  WARNING: Raw SQL detected. Verify it's parameterized."
fi

# Step 4: Commit
git add .
git commit -m "feat(auth): ..."
```

---

## Security Skill — Full Guide

### When to Use Each Skill

| Task | Skill | Time | Details |
|------|-------|------|---------|
| **Quick pre-commit check** | `pre-commit-guard` | 5-10 min | Secrets + NexSynapse IP files only |
| **Full OWASP audit** | `owasp-audit` | 30-45 min | All A01-A10 categories, detailed findings |
| **Secret scanning** | `secret-scanner` | 15-20 min | Deep scan for all types of credentials |
| **Security strategy** | `threat-modeler` | 45-60 min | STRIDE-based threat model + mitigations |
| **Code review** | `code-reviewer` | 20-30 min | SOLID, clean code, security implications |

### How to Load a Skill

**In Claude Code or any AI assistant:**

```bash
# Read the skill file
cat .github/skills/security/pre-commit-guard/SKILL.md

# Follow the "Core Workflow" section (numbered steps)
# Load references on demand from the Reference Guide table
```

**References available in pre-commit-guard skill:**

```bash
cat .github/skills/security/pre-commit-guard/references/scan-rules.md        # All patterns
cat .github/skills/security/pre-commit-guard/references/secret-types.md      # Secret types
cat .github/skills/security/pre-commit-guard/references/remediation.md       # Fixes
```

---

## Checklist Before Every Commit

- [ ] `dotnet build` passes with **zero warnings**
- [ ] `dotnet test` passes with **all tests green** (71+ tests)
- [ ] No hardcoded secrets (Stripe keys, GitHub tokens, AWS keys, passwords)
- [ ] No SQL injection patterns (FromSqlRaw, ExecuteSqlRaw)
- [ ] All new public methods have XML doc comments
- [ ] Localization keys added to SharedResource.resx (if UI changes)
- [ ] Planning docs updated (task-checklist.md, implementation-plan.md)
- [ ] Regulatory check: No "escrow" in user-facing copy (use "secure payment holding")
- [ ] Commit message is conventional format
- [ ] Co-author trailer includes: `Co-authored-by: [AI] <[email]>`

---

## Common Commit Scenarios

### Scenario 1: New Feature (Slice 5 — Login Page)

```bash
# After implementing Login.razor|.cs|.css + LoginCommand handler + tests

# 1. Build & test
dotnet build && dotnet test

# 2. Security scan (read skill, manually scan)
cat .github/skills/security/pre-commit-guard/SKILL.md
# [Follow 5-step core workflow]

# 3. Commit
git add .
git commit -m "feat(auth): implement login page and handler

- Create Components/Pages/Auth/Login.razor|.cs|.css
- Add Features/Auth/Login/LoginCommand and LoginCommandHandler
- Integrate SignInManager<ApplicationUser>.PasswordSignInAsync()
- Add 2 unit tests to validate form binding and error handling
- Update SharedResource.resx with UI strings (Email, Password labels)"
```

### Scenario 2: Security Fix

```bash
git commit -m "security(auth): implement email confirmation requirement

- Change Program.cs: RequireConfirmedEmail = true
- Add email confirmation token generation on register
- Update RegisterPageTests to verify confirmation sent
- Document in docs/cross-cutting/authentication.md"
```

### Scenario 3: Test Improvement

```bash
git commit -m "test(auth): add integration tests for login flow

- Test successful login with valid credentials
- Test failed login with invalid password
- Test account lockout after 5 failed attempts
- Test unauthorized redirect to login page"
```

### Scenario 4: Documentation

```bash
git commit -m "docs(auth): sync ASP.NET Identity setup documentation

- Create docs/cross-cutting/hybrid-identity.md
- Document Actor <-> ApplicationUser mapping
- Add password policy rationale (NIST guidance)
- Add password reset flow TODOs for Phase 2"
```

---

## Troubleshooting

### Pre-Commit Hook Failed: "NexSynapse IP Protection Triggered"

**Cause:** You're trying to commit NexSynapse infrastructure files (AGENTS.md, etc.)

**Fix:**
```bash
# Remove the file from staging
git reset HEAD AGENTS.md

# These files are local-only and should never be committed
# They're in .gitignore for this reason
```

### Pre-Commit Hook Failed: "CRITICAL security issue"

**Cause:** A hardcoded secret was detected

**Fix:**
```bash
# 1. Identify the secret in the file
grep -n "sk_live_\|sk_test_" EscrowApp/Program.cs

# 2. Remove it (move to appsettings.json or user-secrets)
# 3. Unstage and re-stage
git reset HEAD EscrowApp/Program.cs
git add EscrowApp/Program.cs
git commit ...
```

### Want to Bypass the Hook (NOT RECOMMENDED)

```bash
# Only if you're absolutely sure there are no secrets
git commit --no-verify -m "feat: ..."
```

⚠️ **This is a fintech platform — never bypass security checks lightly.**

---

## Setup Instructions

### First-Time Setup (One-Time)

The hook should already be installed at `.git/hooks/pre-commit`. To verify:

```bash
ls -la .git/hooks/pre-commit
```

If not installed:

```bash
# Copy the hook
cp .github/hooks/pre-commit .git/hooks/pre-commit

# Make it executable (macOS/Linux)
chmod +x .git/hooks/pre-commit
```

On **Windows Git Bash**, the hook should work as-is. On **PowerShell**, you may need to adjust your execution policy or use Git Bash for commits.

---

## Integration with Planning Docs

Every commit should advance one task in the planning docs:

1. **Read current status:** `docs/planning/task-checklist.md`
2. **Mark task in-progress:** Update status to `[ ]` → `[x]` (or add new task)
3. **Update implementation-plan.md:** Increment phase completion %
4. **Commit:** Reference the task in your commit message
5. **After commit:** Verify both planning files are in sync

Example:
```bash
git commit -m "feat(auth): implement login page (Slice 5)

Closes #5 in task-checklist.md"
```

---

## Reference

- **Pre-commit hook:** `.git/hooks/pre-commit`
- **Security skills:** `.github/skills/security/`
- **Code quality skills:** `.github/skills/code-quality/`
- **Pre-deployment checklist:** `.github/commands/ship.md`
- **Conventional commits:** https://www.conventionalcommits.org/

---

## Summary

**Before every commit:**

| Step | Command | Time | Tool |
|------|---------|------|------|
| 1 | `dotnet build && dotnet test` | 2-3 min | CLI |
| 2 | Read security skill + scan | 10-15 min | AI Assistant |
| 3 | `git add . && git commit -m "..."` | 2 min | Git |
| **Total** | | **15-20 min** | |

**Hook runs automatically** → blocks on CRITICAL issues → ✅ commit succeeds

