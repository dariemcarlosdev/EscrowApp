---
name: portable-infrastructure
description: "Create and apply NexSynapse portable AI infrastructure to any project — sync rules, security hooks, templates, and cross-platform compatibility"
license: MIT
allowed-tools: Read, Edit, Create, Bash, PowerShell
metadata:
  version: "1.0.0"
  domain: infrastructure
  triggers: portable infrastructure, nexsynapse integration, ai portability, cross-platform sync
  role: architect
  scope: implementation
  platforms: copilot-cli, claude, gemini, codex
  output-format: infrastructure
  related-skills: documentation-organizer, ci-cd-builder, owasp-audit
---

# Portable Infrastructure Skill

Transform any project into a portable AI-enabled codebase with NexSynapse infrastructure — bidirectional sync, security patterns, and cross-platform compatibility.

## When to Use This Skill

- Setting up a new project with AI assistant compatibility
- Migrating an existing project to NexSynapse portable infrastructure
- Ensuring AI configurations work across GitHub Copilot CLI, Claude, Gemini, Codex
- Implementing security patterns for AI-assisted development
- Creating reusable infrastructure that works across projects

## Core Workflow

1. **Assess Current State** — Examine existing AI configurations and infrastructure patterns
   - ✅ Checkpoint: Inventory of .copilot/, .claude/, hooks, workflows documented

2. **Install NexSynapse Templates** — Copy pre-commit hooks, workflows, and configurations
   - ✅ Checkpoint: Templates installed and executable permissions set

3. **Configure Sync Rules** — Set up bidirectional .copilot ↔ .claude portability enforcement
   - ✅ Checkpoint: Sync validation passes in strict mode

4. **Integrate Security Patterns** — Install secret scanning, IP protection, OWASP-aware hooks
   - ✅ Checkpoint: Security hooks block test secrets successfully

5. **Validate Cross-Platform** — Test configurations work across all supported AI platforms
   - ✅ Checkpoint: Skills load successfully on each platform

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Sync Rule Specification | `references/sync-rule-spec.md` | Understanding the portable architecture principle |
| Template Integration | `references/template-integration.md` | Installing hooks, workflows, and configurations |
| Security Patterns | `references/security-patterns.md` | Setting up AI-aware security scanning |
| Cross-Platform Testing | `references/cross-platform-testing.md` | Validating compatibility across AI tools |

## Quick Reference

```bash
# 1. Install NexSynapse infrastructure (from project root)
cp ../NexSynapse/templates/git-hooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

cp ../NexSynapse/templates/workflows/ai-config-sync.yml .github/workflows/

# 2. Test sync validation
../NexSynapse/scripts/ai-config-sync.sh "$(pwd)" --validate --strict

# 3. Verify security patterns
echo "sk_live_test123" > test-secret.txt
git add test-secret.txt
git commit -m "test security"  # Should be blocked

# 4. Clean up test
rm test-secret.txt
git reset HEAD .
```

## Key Benefits

1. **Universal AI Compatibility** — Works with GitHub Copilot CLI, Claude, Gemini, Codex simultaneously
2. **Automatic Sync Enforcement** — Changes to one AI platform automatically sync to others
3. **Security-First** — Prevents secret leaks and IP exposure through automated scanning
4. **Zero Configuration** — Templates provide ready-to-use infrastructure
5. **Project Portability** — Same infrastructure works across any project type

## Infrastructure Components

### Pre-Commit Security Hook
- **Purpose**: Block commits with secrets, enforce AI sync rules
- **Location**: `.git/hooks/pre-commit`
- **Features**: 6 critical security patterns + AI configuration validation

### Bidirectional Sync Scripts
- **Purpose**: Translate AI configurations between platforms
- **Location**: `../NexSynapse/scripts/ai-config-sync.{sh,ps1}`
- **Features**: Bash ↔ PowerShell, YAML ↔ JSON, validation + auto-sync

### GitHub Actions Workflow
- **Purpose**: CI validation of portable AI architecture
- **Location**: `.github/workflows/ai-config-sync.yml`
- **Features**: Drift detection, JSON validation, security scanning

### Universal Skills System
- **Purpose**: Skills work identically across all AI platforms
- **Location**: `.github/skills/` (source) + platform bridges
- **Features**: Progressive disclosure, YAML metadata, cross-references

## Constraints

### MUST DO
- Install ALL NexSynapse templates — partial installation breaks portability
- Test sync validation before declaring integration complete
- Verify security hooks block test secrets before committing real code
- Update project documentation to reference NexSynapse infrastructure
- Run health check after installation to validate all components

### MUST NOT
- Skip security pattern installation — AI-assisted development requires protection
- Hard-code NexSynapse paths — use relative paths that work from any project
- Mix old and new infrastructure — fully migrate to NexSynapse or don't integrate
- Commit NexSynapse IP files to project repositories — they're local-only
- Ignore sync validation failures — portability depends on compliance

## Success Criteria

✅ **Pre-commit hook blocks secrets**: `echo "sk_test_123" | git commit` fails  
✅ **Sync validation passes**: `../NexSynapse/scripts/ai-config-sync.sh --validate --strict`  
✅ **GitHub Actions pass**: Workflow validates AI configurations in CI  
✅ **Skills load on all platforms**: Bridge files successfully reference universal skills  
✅ **Documentation updated**: Project README references NexSynapse infrastructure

## Output Template

After successful integration:

```
Project Structure:
├── .git/hooks/pre-commit          ← NexSynapse security + sync enforcement
├── .github/workflows/             ← CI validation
│   └── ai-config-sync.yml
├── .copilot/                      ← AI configurations (synced)
├── .claude/                       ← AI configurations (synced)  
├── .github/skills/                ← Universal skills (if applicable)
└── README.md                      ← Updated with NexSynapse references

Integration Status:
✅ Security patterns active
✅ Sync rules enforced  
✅ Cross-platform compatibility validated
✅ CI pipeline integrated
✅ Documentation updated

Next Steps:
1. Test AI assistant functionality on each platform
2. Add project-specific skills to .github/skills/ if needed
3. Customize security patterns for project-specific requirements
4. Share NexSynapse integration pattern with team
```