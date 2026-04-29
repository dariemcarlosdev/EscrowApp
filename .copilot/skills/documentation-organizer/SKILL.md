---
name: documentation-organizer
description: "Organize project documentation using module-based hierarchy for accelerated context discovery and developer onboarding. Eliminates documentation archaeology."
license: MIT
allowed-tools: Read, Write, Create, Edit, Grep, Glob, Bash, Powershell
metadata:
  version: "1.0.0"
  domain: workflow
  triggers: organize docs, documentation structure, module organization, doc hierarchy, context discovery
  role: information-architect
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: structure
  related-skills: readme-generator, adr-creator, api-documenter, deep-context-generator
---

# Documentation Organizer

Transform scattered project documentation into a module-based hierarchy that accelerates context discovery and eliminates documentation archaeology. Organizes features by business concern rather than technical category.

## When to Use This Skill

- Project documentation has grown organically in flat structures (features/, docs/, etc.)
- Developers waste time hunting for related information across scattered directories
- Onboarding new team members who struggle to find relevant documentation
- Documentation spans multiple concerns but lacks logical grouping
- Need to establish maintainable documentation patterns for growing codebases
- Preparing for team scaling where fast context discovery becomes critical

## Core Workflow

### 1. **Documentation Audit** — Map existing structure and identify grouping opportunities
   - ✅ Checkpoint: Current structure documented with pain points identified

### 2. **Module Strategy Design** — Define business-concern-based grouping strategy
   - ✅ Checkpoint: Module hierarchy designed with clear concern boundaries

### 3. **Structure Creation** — Create new module-based folder hierarchy  
   - ✅ Checkpoint: New folder structure created following module pattern

### 4. **Content Migration** — Move documentation to appropriate module locations
   - ✅ Checkpoint: All documentation files relocated without loss

### 5. **Navigation Enhancement** — Create navigation aids and update references
   - ✅ Checkpoint: README index created, internal links updated, developer onboarding accelerated

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Module Strategy | `references/module-strategy.md` | Designing concern-based groupings |
| Migration Patterns | `references/migration-patterns.md` | Moving files and updating links |
| Navigation Design | `references/navigation-design.md` | Creating indexes and discovery aids |
| Maintenance Workflows | `references/maintenance-workflows.md` | Keeping organization current |

## Key Benefits

### 🚀 **Faster Context Discovery**
- **Before:** Developers search across scattered directories for 5-15 minutes
- **After:** Direct navigation to relevant module in seconds
- **Pattern:** `docs/modules/authentication/` contains ALL auth-related docs

### 🏗️ **Logical Grouping** 
- **Before:** Related features split across `features/`, `cross-cutting/`, `architecture/`
- **After:** Complete feature context co-located in single module directory
- **Pattern:** Authentication login + registration + setup + patterns in one place

### 📈 **Scalable Organization**
- **Before:** New features create more scattered documentation
- **After:** Clear module boundaries guide placement of new documentation  
- **Pattern:** New payment feature → `docs/modules/payments/new-feature/`

### 🧭 **Clear Navigation**
- **Before:** No systematic way to discover what documentation exists
- **After:** README index provides instant access to any documentation
- **Pattern:** Module-first navigation eliminates documentation archaeology

## Module Organization Pattern

```
docs/
├── modules/                     # Business modules (NEW)
│   ├── {business-concern}/      # e.g., authentication, payments, ui
│   │   ├── {feature-name}/      # Individual feature documentation
│   │   ├── {cross-cutting}/     # Concern-specific patterns
│   │   └── README.md           # Module navigation index
│   └── system/                  # Cross-cutting technical concerns
│       ├── {framework}/         # e.g., validation, localization
│       └── README.md           # System concerns index
├── platform/                   # Platform architecture and operations
│   ├── architecture/           # System design patterns
│   ├── operations/             # Deployment and monitoring
│   └── business/              # Business model and compliance
├── audits/                     # Security and compliance audits
├── planning/                   # Project execution tracking
├── README.md                   # Master navigation index (CRITICAL)
└── {inventory-file}            # Feature/component inventory
```

## Quick Reference

```bash
# Example: Organize EscrowApp documentation
# 1. Audit current structure
find docs/ -name "*.md" -type f | head -20

# 2. Design modules by business concern
# Authentication: login, registration, identity patterns
# Payments: hold, release, dispute, fees  
# UI: dashboards, components, user experiences
# System: validation, localization, testing

# 3. Create new structure
mkdir -p docs/modules/{authentication,payments,ui,system}
mkdir -p docs/platform/{architecture,operations,business}

# 4. Migrate with preservation
mv docs/features/user-login docs/modules/authentication/
mv docs/cross-cutting/localization docs/modules/system/

# 5. Create navigation
# Generate README.md with module links
# Update internal documentation references
```

## Constraints

### MUST DO
- Preserve all existing documentation content during migration
- Create comprehensive README.md navigation index for instant access
- Update all internal links and cross-references to new locations
- Group by **business concern first**, then by technical concern
- Maintain clear module boundaries — no overlap between concerns
- Include migration rationale and benefits in reorganization documentation

### MUST NOT
- Move files without updating internal references (breaks navigation)
- Create modules with unclear or overlapping boundaries
- Eliminate existing documentation (migration, not deletion)
- Organize by technical structure alone (controllers/, services/, etc.)
- Skip creating navigation aids (defeats the purpose)
- Make the new structure less discoverable than the original

## Success Metrics

### Developer Experience
- **Context Discovery Time:** Reduced from minutes to seconds
- **Onboarding Speed:** New developers find relevant docs immediately
- **Maintenance Efficiency:** Clear placement rules for new documentation

### Information Architecture  
- **Logical Cohesion:** Related information co-located in modules
- **Scalable Growth:** New features have obvious placement in existing modules
- **Navigation Clarity:** README provides instant access to any documentation

---

**Pattern Validated:** Module-based documentation organization eliminates archaeology and accelerates developer productivity through predictable navigation paths.