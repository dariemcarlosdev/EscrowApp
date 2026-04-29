# Project .claude Location Templates

## Purpose

Guide for where `.claude` folders can be located in different project structures, tech stacks, and organizational patterns. Use this to:

- Identify where `.claude` should go in your project
- Understand conventional locations for your tech stack
- Set up `.claude` in monorepos, workspaces, and complex structures
- Create a `.claude` template matching your project layout

---

## Single Project Structure

### Standard Web/API Project

```
project-root/
├── .claude/                          ← .claude folder here
│   ├── manifest.json
│   ├── settings.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── CODEX.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── code-quality/
│   │   └── ...
│   ├── bridges/
│   ├── extensions/
│   └── .gitignore
│
├── src/                              ← Source code
│   ├── api/
│   ├── models/
│   ├── services/
│   └── ...
│
├── tests/
├── docs/
├── .git/
├── .env
├── package.json
├── tsconfig.json
├── README.md
└── .gitignore
```

**Standard for:**
- Single monolithic applications
- Small to medium projects
- Shared .claude across team

### .NET / ASP.NET Core Project

```
EscrowApp/
├── .claude/                          ← .claude folder here
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   │   └── claude-export/
│   │   ├── security/
│   │   │   └── owasp-audit/
│   │   ├── architecture/
│   │   ├── testing/
│   │   └── ...
│   └── extensions/
│
├── EscrowApp/                        ← Main project
│   ├── Components/
│   ├── Features/
│   ├── Models/
│   ├── Services/
│   ├── Program.cs
│   ├── appsettings.json
│   └── EscrowApp.csproj
│
├── EscrowApp.Tests/                  ← Test project
│   ├── Features/
│   ├── Models/
│   └── EscrowApp.Tests.csproj
│
├── EscrowApp.sln
├── README.md
└── .gitignore
```

**Standard for:**
- .NET solutions with multiple projects
- Enterprise applications
- Teams using Visual Studio / Rider

### Node.js / TypeScript Project

```
my-app/
├── .claude/                          ← .claude folder here
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   └── ...
│   └── extensions/
│
├── src/
│   ├── api/
│   ├── components/
│   ├── services/
│   ├── types/
│   └── index.ts
│
├── tests/
├── public/
├── node_modules/
├── package.json
├── tsconfig.json
├── .env.local
├── README.md
└── .gitignore
```

**Standard for:**
- Node.js/Deno projects
- React/Vue/Svelte frontends
- Full-stack JavaScript applications

### Python Project

```
project/
├── .claude/                          ← .claude folder here
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   └── ...
│   └── extensions/
│
├── src/
│   ├── __init__.py
│   ├── main.py
│   ├── models/
│   ├── services/
│   └── utils/
│
├── tests/
├── venv/                             ← Virtual env (in .gitignore)
├── requirements.txt
├── setup.py
├── pyproject.toml
├── README.md
└── .gitignore
```

**Standard for:**
- Python/Django/FastAPI projects
- Data science / ML projects
- CLI tools

---

## Monorepo Structures

### Single Root `.claude` (Shared Skills)

Best when: All projects share same skills

```
monorepo/
├── .claude/                          ← SHARED: All projects use this
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── code-quality/
│   │   └── ...
│   ├── bridges/
│   └── extensions/
│
├── packages/
│   ├── api/
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   ├── web/
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   └── lib/
│       ├── src/
│       ├── tests/
│       └── package.json
│
├── .gitignore
├── package.json (root)
├── pnpm-workspace.yaml
└── README.md
```

**Advantages:**
- Single source of truth for skills
- Easier to maintain consistency
- Smaller total size (no duplication)

**Disadvantages:**
- All packages must agree on skills
- Harder to customize per-package

---

### Per-Package `.claude` (Customized Skills)

Best when: Each package has different skill needs

```
monorepo/
├── packages/
│   ├── api/
│   │   ├── .claude/                  ← API-specific skills
│   │   │   ├── manifest.json
│   │   │   ├── CLAUDE.md
│   │   │   ├── skills/
│   │   │   │   ├── ai/
│   │   │   │   ├── security/
│   │   │   │   ├── database/
│   │   │   │   └── devops/
│   │   │   └── extensions/
│   │   │
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   ├── web/
│   │   ├── .claude/                  ← Web-specific skills
│   │   │   ├── manifest.json
│   │   │   ├── CLAUDE.md
│   │   │   ├── skills/
│   │   │   │   ├── ai/
│   │   │   │   ├── code-quality/
│   │   │   │   └── testing/
│   │   │   └── extensions/
│   │   │
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   └── lib/
│       ├── .claude/                  ← Lib-specific skills
│       ├── src/
│       └── package.json
│
├── .gitignore
├── package.json (root)
└── README.md
```

**Advantages:**
- Each package can have tailored skills
- Granular control per team/package
- Clear separation of concerns

**Disadvantages:**
- Duplication of skills across packages
- Harder to sync versions
- Larger total size

---

### Hybrid: Root + Per-Package `.claude`

Best when: Shared base + customizations

```
monorepo/
├── .claude/                          ← ROOT: Shared base skills
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/                       (shared by all)
│   │   ├── security/                 (shared by all)
│   │   ├── code-quality/             (shared by all)
│   │   └── ...
│   └── extensions/
│
├── packages/
│   ├── api/
│   │   ├── .claude/                  ← API OVERRIDES: Extra skills
│   │   │   ├── manifest.json
│   │   │   ├── skills/
│   │   │   │   ├── database/         (API-specific)
│   │   │   │   └── devops/           (API-specific)
│   │   │   └── .claude-inherited     (marker: inherits from root)
│   │   │
│   │   ├── src/
│   │   └── package.json
│   │
│   └── web/
│       ├── .claude/                  ← WEB OVERRIDES: Extra skills
│       │   ├── manifest.json
│       │   ├── skills/
│       │   │   └── devops/           (Web-specific)
│       │   └── .claude-inherited
│       │
│       ├── src/
│       └── package.json
│
├── .gitignore
└── package.json (root)
```

**Advantages:**
- Base skills shared (no duplication)
- Per-package customization
- Clear inheritance model

**How it works:**
1. Claude IDE loads root `.claude/` first
2. If package has `.claude/`, loads and merges
3. Package skills override root skills with same name

---

## Multi-Workspace Structures

### Workspace-Level `.claude` (Shared by Workspace)

```
workspace/
├── .claude/                          ← WORKSPACE-LEVEL: Shared by all projects
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── code-quality/
│   │   └── ...
│   └── extensions/
│
├── project-a/
│   ├── src/
│   ├── tests/
│   └── package.json
│
├── project-b/
│   ├── src/
│   ├── tests/
│   └── package.json
│
├── project-c/
│   ├── src/
│   ├── tests/
│   └── package.json
│
├── workspace.code-workspace          ← VS Code workspace file
├── .gitignore
└── README.md
```

**Use when:**
- Multiple independent projects in one folder
- Team works on multiple projects together
- Shared tooling and standards

---

## Alternative `.claude` Locations

### In `.vscode` (VS Code Specific)

```
project/
├── .vscode/
│   ├── claude/                       ← Alternative: Claude config in .vscode
│   │   ├── manifest.json
│   │   ├── CLAUDE.md
│   │   ├── skills/
│   │   │   ├── ai/
│   │   │   └── ...
│   │   └── extensions/
│   │
│   ├── settings.json
│   ├── launch.json
│   └── extensions.json
│
├── src/
├── tests/
└── README.md
```

**Use when:**
- VS Code exclusive (not IDE agnostic)
- Want all IDE config in one folder
- **Note:** Not recommended — prefer root `.claude/`

---

### In `.config` or `config` Directory

```
project/
├── config/
│   ├── claude/                       ← Alternative: In config directory
│   │   ├── manifest.json
│   │   ├── CLAUDE.md
│   │   ├── skills/
│   │   │   ├── ai/
│   │   │   └── ...
│   │   └── extensions/
│   │
│   ├── eslint.config.js
│   ├── prettier.config.js
│   └── webpack.config.js
│
├── src/
├── tests/
└── README.md
```

**Use when:**
- All config centralized in `config/`
- Want to group `.claude` with other tool configs
- **Note:** Less discoverable — Claude IDE expects root `.claude/`

---

### In `docs` or `.docs`

```
project/
├── docs/
│   ├── .claude/                      ← Documentation-integrated
│   │   ├── manifest.json
│   │   ├── CLAUDE.md
│   │   ├── skills/
│   │   │   ├── ai/
│   │   │   └── ...
│   │   └── extensions/
│   │
│   ├── architecture/
│   ├── guides/
│   └── api/
│
├── src/
├── tests/
└── README.md
```

**Use when:**
- `.claude` tightly integrated with docs
- Documentation-driven development
- **Note:** Not discoverable — requires manual path configuration

---

## Complex Project Examples

### Full-Stack Web Application

```
ecommerce-platform/
├── .claude/                          ← MAIN: Shared by entire project
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── architecture/
│   │   ├── code-quality/
│   │   ├── testing/
│   │   ├── database/
│   │   ├── devops/
│   │   └── documentation/
│   └── extensions/
│
├── backend/
│   ├── .claude/                      ← OPTIONAL: Backend-specific overrides
│   │   ├── manifest.json
│   │   ├── skills/
│   │   │   ├── database/             (overrides root)
│   │   │   └── devops/               (overrides root)
│   │   └── .claude-inherited
│   │
│   ├── src/
│   │   ├── api/
│   │   ├── models/
│   │   ├── services/
│   │   └── ...
│   │
│   ├── tests/
│   ├── requirements.txt
│   └── README.md
│
├── frontend/
│   ├── .claude/                      ← OPTIONAL: Frontend-specific overrides
│   │   ├── manifest.json
│   │   ├── skills/
│   │   │   ├── devops/               (overrides root)
│   │   │   └── testing/              (overrides root)
│   │   └── .claude-inherited
│   │
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   └── ...
│   │
│   ├── tests/
│   ├── package.json
│   └── README.md
│
├── shared/
│   ├── types/
│   ├── utils/
│   └── README.md
│
├── .git/
├── docker-compose.yml
├── .gitignore
└── README.md
```

### Monorepo with Workspaces (npm/pnpm)

```
nexus/                                 ← Monorepo root
├── .claude/                          ← MONOREPO: All packages share
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── architecture/
│   │   ├── code-quality/
│   │   └── testing/
│   └── extensions/
│
├── packages/
│   ├── auth/
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   ├── api/
│   │   ├── .claude/                  ← API-specific: Database, DevOps
│   │   │   ├── manifest.json
│   │   │   ├── skills/
│   │   │   │   ├── database/
│   │   │   │   └── devops/
│   │   │   └── .claude-inherited
│   │   │
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   ├── web/
│   │   ├── .claude/                  ← Web-specific: DevOps, Testing
│   │   │   ├── manifest.json
│   │   │   ├── skills/
│   │   │   │   ├── devops/
│   │   │   │   └── testing/
│   │   │   └── .claude-inherited
│   │   │
│   │   ├── src/
│   │   ├── tests/
│   │   └── package.json
│   │
│   └── cli/
│       ├── src/
│       ├── tests/
│       └── package.json
│
├── .gitignore
├── pnpm-workspace.yaml
├── package.json (root)
└── README.md
```

### Enterprise Monorepo (Multiple Teams)

```
enterprise/                            ← Org root
├── .claude/                          ← ORG-LEVEL: Core skills for all teams
│   ├── manifest.json
│   ├── CLAUDE.md
│   ├── GEMINI.md
│   ├── skills/
│   │   ├── ai/
│   │   ├── security/
│   │   ├── architecture/
│   │   ├── code-quality/
│   │   ├── testing/
│   │   ├── compliance/               (org-wide standards)
│   │   └── documentation/
│   └── extensions/
│
├── teams/
│   │
│   ├── platform/
│   │   ├── .claude/                  ← PLATFORM TEAM: Infrastructure focus
│   │   │   ├── manifest.json
│   │   │   ├── skills/
│   │   │   │   ├── database/         (platform-specific)
│   │   │   │   ├── devops/           (platform-specific)
│   │   │   │   └── monitoring/       (platform-specific)
│   │   │   └── .claude-inherited
│   │   │
│   │   ├── services/
│   │   │   ├── auth-service/
│   │   │   ├── payment-service/
│   │   │   └── user-service/
│   │   │
│   │   └── shared/
│   │
│   ├── product/
│   │   ├── .claude/                  ← PRODUCT TEAM: Feature development focus
│   │   │   ├── manifest.json
│   │   │   ├── skills/
│   │   │   │   ├── architecture/     (product-specific)
│   │   │   │   └── testing/          (product-specific)
│   │   │   └── .claude-inherited
│   │   │
│   │   ├── projects/
│   │   │   ├── mobile/
│   │   │   ├── web/
│   │   │   └── desktop/
│   │   │
│   │   └── shared/
│   │
│   └── data/
│       ├── .claude/                  ← DATA TEAM: Analytics focus
│       │   ├── manifest.json
│       │   ├── skills/
│       │   │   ├── database/         (data-specific)
│       │   │   └── devops/           (data-specific)
│       │   └── .claude-inherited
│       │
│       ├── pipelines/
│       ├── warehouses/
│       └── shared/
│
├── docs/
│   └── standards/
│
├── .gitignore
├── .github/
└── README.md
```

---

## Quick Selection Guide

**Choose `.claude` location based on:**

| Structure | Recommended Location | Reasoning |
|-----------|---------------------|-----------|
| **Single project** | `project/.claude/` | Standard, discoverable, clean |
| **Monorepo (shared)** | `monorepo/.claude/` | All packages share skills |
| **Monorepo (per-package)** | `packages/api/.claude/` + `packages/web/.claude/` | Custom skills per package |
| **Monorepo (hybrid)** | Root + `packages/X/.claude/` | Base + overrides |
| **Multi-workspace** | `workspace/.claude/` | Shared by independent projects |
| **Workspace + overrides** | Root + `project-a/.claude/` | Base skills + project customization |
| **Enterprise** | `org/.claude/` + `teams/X/.claude/` | Org-wide + team-specific standards |

---

## .gitignore Rules

Add to your `.gitignore` based on location:

### Root `.claude` (commit to repo)
```gitignore
# Allow .claude in version control
!.claude/
!.claude/**

# But ignore generated files
.claude/temp/
.claude/*.log
.claude/cache/
```

### Per-package `.claude` (selective commit)
```gitignore
# Allow .claude in specific packages
!packages/api/.claude/
!packages/api/.claude/**

!packages/web/.claude/
!packages/web/.claude/**

# Ignore generated/temp files
packages/**/.claude/temp/
packages/**/.claude/*.log
```

### Do NOT commit (use local only)
```gitignore
# Ignore all .claude folders (local Claude IDE setup only)
.claude/
packages/**/.claude/
```

---

## Template Checklist

Before placing `.claude` in your project, verify:

- [ ] Selected location matches your project structure (single, monorepo, workspace, etc.)
- [ ] Location is discoverable by Claude IDE and other tools
- [ ] `.gitignore` configured to allow/ignore appropriately
- [ ] Team agrees on shared vs. per-package setup
- [ ] Documented location in project `README.md`
- [ ] Created `.claude/manifest.json` with inventory
- [ ] Added `.claude/CLAUDE.md` and `.claude/GEMINI.md` bridges
- [ ] Organized skills in `.claude/skills/{category}/{skillName}/SKILL.md`
- [ ] Tested skill loading in Claude IDE

