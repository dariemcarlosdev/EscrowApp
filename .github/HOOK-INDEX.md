# Hook Infrastructure — Quick Navigation Guide

**Status:** ✅ Complete and ready for deployment  
**Updated:** 2026-04-16 02:10 UTC

---

## 📍 Where to Start?

### I'm a Developer
**I want to...**

- ✅ **Get started immediately** → Read: `.github/hooks/SETUP-AND-VERIFY.md`
- ✅ **Understand the workflow** → Read: `.github/PORTABLE-COMMIT-WORKFLOW.md`
- ✅ **Understand how it works** → Read: `.github/INFRASTRUCTURE-OVERVIEW.md`

**Quick start:**
```bash
bash .github/hooks/setup-pre-commit.sh
# Then commit as usual — hooks run automatically
```

---

### I'm a Backend/DevOps Team Member
**I want to...**

- ✅ **Maintain parity** → Read: `.github/PARITY-GUIDE.md`
- ✅ **Add new security patterns** → See `.github/PARITY-GUIDE.md` → "How to Add a New Security Check"
- ✅ **Understand the infrastructure** → Read: `.github/HOOK-INFRASTRUCTURE-SUMMARY.md`
- ✅ **Troubleshoot issues** → Read: `.github/hooks/SETUP-AND-VERIFY.md` → "Troubleshooting"

**Key responsibility:** Keep shell script, Copilot config, and Claude config synchronized

---

### I'm an AI Assistant (Claude Code, Copilot, Gemini)
**I want to...**

- ✅ **Understand what to do before committing** → Read: `.github/PORTABLE-COMMIT-WORKFLOW.md`
- ✅ **Help users set up hooks** → Link them to: `.github/hooks/SETUP-AND-VERIFY.md`
- ✅ **Run pre-commit validation** → Available as `/pre-commit-validate` command or tool
- ✅ **Maintain infrastructure parity** → Refer backend team to: `.github/PARITY-GUIDE.md`

---

## 📚 Document Map

### Infrastructure Overview (Start Here)
```
.github/INFRASTRUCTURE-OVERVIEW.md
├─ Quick explanation of what this is
├─ How it works (3 layers)
├─ File structure
├─ Parity principle
├─ Usage examples
├─ Maintenance checklist
└─ Troubleshooting
```

### User Guide (Workflows)
```
.github/PORTABLE-COMMIT-WORKFLOW.md
├─ Quick start
├─ Three-layer security
├─ Full recommended workflow
├─ Environment-specific workflows
├─ Security skill reference
├─ Planning docs integration
└─ Definition of done
```

### Setup & Testing (Installation)
```
.github/hooks/SETUP-AND-VERIFY.md
├─ Quick install (one command)
├─ Manual install (step by step)
├─ Verification checklist
├─ How to test the hooks
├─ Troubleshooting
├─ IDE integration
└─ Next steps
```

### Parity Guide (Maintenance)
```
.github/PARITY-GUIDE.md
├─ Architecture overview
├─ Files & responsibilities
├─ How to maintain parity
├─ Parity checklist
├─ How to add a new security check
├─ Environment-specific variations
├─ Testing parity
├─ Rollback procedure
└─ Future enhancements
```

### Summary (This Document)
```
.github/HOOK-INFRASTRUCTURE-SUMMARY.md
├─ What was built (files created)
├─ How it works (user perspective)
├─ Parity guarantee
├─ Security checks implemented
├─ Setup instructions
├─ Architecture principles
├─ Integration points
├─ Testing & verification
└─ Maintenance checklist
```

---

## 🔧 Core Files

| File | Type | Role | Audience |
|------|------|------|----------|
| `.github/hooks/pre-commit` | Shell Script | Source of truth for patterns | DevOps (editing) |
| `.github/hooks/pre-commit.yaml` | Config | Copilot CLI integration | System (auto-used) |
| `.github/hooks/setup-pre-commit.sh` | Script | One-command setup | All users |
| `.claude/hooks/pre-commit.yaml` | Config | Claude Code integration | System (auto-used) |
| `.claude/settings.json` | Config | Claude Code registration | System (auto-used) |

---

## ⚡ Quick Commands

```bash
# Setup (one-time)
bash .github/hooks/setup-pre-commit.sh

# Test hook
bash .github/hooks/pre-commit

# Verify setup
[ -x .git/hooks/pre-commit ] && echo "✅ Hook installed"

# See what hooks do
cat .github/hooks/pre-commit

# See Copilot config
cat .github/hooks/pre-commit.yaml

# See Claude config
cat .claude/hooks/pre-commit.yaml
```

---

## 📋 Checklist: New Developer Setup

- [ ] Read: `.github/INFRASTRUCTURE-OVERVIEW.md` (5 min)
- [ ] Read: `.github/PORTABLE-COMMIT-WORKFLOW.md` (10 min)
- [ ] Run: `bash .github/hooks/setup-pre-commit.sh`
- [ ] Test: Follow `.github/hooks/SETUP-AND-VERIFY.md` → "How to Test the Hooks"
- [ ] Verify: Run the verification checklist
- [ ] Ready to commit!

**Total time: ~20 minutes**

---

## 📋 Checklist: Backend/DevOps Setup

- [ ] Read: `.github/INFRASTRUCTURE-OVERVIEW.md`
- [ ] Read: `.github/PARITY-GUIDE.md`
- [ ] Bookmark: `.github/PARITY-GUIDE.md` → "How to Add a New Security Check"
- [ ] Test: Run `.github/hooks/SETUP-AND-VERIFY.md` → "Manual Test"
- [ ] Review: Parity checklist before any changes
- [ ] Maintain: Follow parity rules on every update

---

## 🎯 Common Tasks

### Task: I Want to Add a New Security Pattern

1. **Read:** `.github/PARITY-GUIDE.md` → "How to Add a New Security Check"
2. **Update:** `.github/hooks/pre-commit` (shell script)
3. **Mirror:** `.github/hooks/pre-commit.yaml` (Copilot config)
4. **Mirror:** `.claude/hooks/pre-commit.yaml` (Claude config)
5. **Test:** All three implementations with sample pattern
6. **Commit:** Single atomic commit

**Estimated time:** 30-45 minutes

---

### Task: I Need to Troubleshoot Hook Issues

1. **Check:** Is hook installed? `ls -la .git/hooks/pre-commit`
2. **Check:** Is it executable? `[ -x .git/hooks/pre-commit ] && echo "✅"`
3. **Reinstall:** `bash .github/hooks/setup-pre-commit.sh`
4. **Test:** `bash .github/hooks/pre-commit` with test file
5. **Read:** `.github/hooks/SETUP-AND-VERIFY.md` → "Troubleshooting"

---

### Task: Hook Blocks Code I Need to Commit

1. **Check:** What pattern matched? (look at error message)
2. **Verify:** Is it a real secret or false positive?
3. **Fix:** Remove the secret if real, or...
4. **Bypass:** `git commit --no-verify` (only if 100% sure)
5. **Report:** If false positive, see `.github/PARITY-GUIDE.md` → Rollback

---

## 🔗 References

- **Setup:** `.github/hooks/SETUP-AND-VERIFY.md`
- **Workflow:** `.github/PORTABLE-COMMIT-WORKFLOW.md`
- **Infrastructure:** `.github/INFRASTRUCTURE-OVERVIEW.md`
- **Maintenance:** `.github/PARITY-GUIDE.md`
- **Summary:** `.github/HOOK-INFRASTRUCTURE-SUMMARY.md`

---

## ✅ Status

| Component | Status | Details |
|-----------|--------|---------|
| Git Hook | ✅ Ready | `.github/hooks/pre-commit` |
| Setup Script | ✅ Ready | `.github/hooks/setup-pre-commit.sh` |
| Copilot Config | ✅ Ready | `.github/hooks/pre-commit.yaml` |
| Claude Config | ✅ Ready | `.claude/hooks/pre-commit.yaml` |
| Documentation | ✅ Complete | 5 comprehensive guides |
| Testing | ✅ Ready | See SETUP-AND-VERIFY.md |

**Ready for deployment.** Users can run setup immediately.

---

## 📞 Questions?

- **How do I set up?** → `.github/hooks/SETUP-AND-VERIFY.md`
- **How do I use it?** → `.github/PORTABLE-COMMIT-WORKFLOW.md`
- **How does it work?** → `.github/INFRASTRUCTURE-OVERVIEW.md`
- **How do I maintain it?** → `.github/PARITY-GUIDE.md`
- **What was built?** → `.github/HOOK-INFRASTRUCTURE-SUMMARY.md`

