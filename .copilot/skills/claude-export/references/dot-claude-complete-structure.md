# .claude Folder Structure Reference

## Complete Directory Map

The `.claude` folder is the Claude IDE configuration directory. This reference shows **all possible subfolders and files** it can contain, organized by purpose.

---

## Canonical Structure (All Possibilities)

```
project/.claude/
│
├── 📄 MANIFEST FILES (Root level, required for structure awareness)
│   ├── manifest.json                 ← Inventory of all skills, bridges, extensions (REQUIRED)
│   ├── settings.json                 ← Claude IDE project-specific settings
│   ├── .structure.json               ← Structure metadata (format, layout version)
│   ├── .claude-inherited             ← Marker file: inherits from parent .claude (monorepo)
│   └── README.md                     ← Optional: Custom .claude documentation
│
├── 🌉 MODEL BRIDGES (Root level, instructions for each model)
│   ├── CLAUDE.md                     ← Instructions/context for Claude models
│   ├── GEMINI.md                     ← Instructions/context for Gemini models
│   ├── CODEX.md                      ← Instructions/context for Codex/Ollama
│   ├── OpenAI.md                     ← Instructions/context for GPT-4/gpt-4o
│   ├── Anthropic.md                  ← Instructions for Anthropic models
│   ├── Google.md                     ← Instructions for Google models
│   └── Local.md                      ← Instructions for local/self-hosted models
│
├── 🎯 SKILLS DIRECTORY (Organized by category)
│   └── skills/
│       ├── ai/                       ← AI orchestration, prompts, agents
│       │   ├── agent-orchestrator/
│       │   │   ├── SKILL.md          ← Main skill file (REQUIRED per skill)
│       │   │   ├── manifest.json     ← Skill-level metadata (optional)
│       │   │   └── references/       ← Supporting docs (optional)
│       │   │       ├── delegation-patterns.md
│       │   │       ├── context-minimization.md
│       │   │       └── ...
│       │   │
│       │   ├── claude-export/
│       │   │   ├── SKILL.md
│       │   │   ├── README.md
│       │   │   └── references/
│       │   │       ├── structure-conventions.md
│       │   │       ├── project-structure-templates.md
│       │   │       └── ...
│       │   │
│       │   ├── mcp-developer/
│       │   ├── prompt-engineer/
│       │   └── ...
│       │
│       ├── security/                 ← Security, OWASP, auth, encryption
│       │   ├── owasp-audit/
│       │   ├── authentication/
│       │   ├── authorization/
│       │   ├── threat-modeler/
│       │   └── ...
│       │
│       ├── architecture/             ← System design, patterns, reviews
│       │   ├── architecture-reviewer/
│       │   ├── design-pattern-advisor/
│       │   ├── legacy-modernizer/
│       │   └── ...
│       │
│       ├── code-quality/             ← Code review, refactoring, quality
│       │   ├── code-reviewer/
│       │   ├── refactor-planner/
│       │   ├── debugging-wizard/
│       │   ├── quality-analyzer/
│       │   ├── tech-debt-tracker/
│       │   └── ...
│       │
│       ├── testing/                  ← Test generation, TDD, coverage
│       │   ├── test-generator/
│       │   ├── tdd-coach/
│       │   ├── test-coverage-analyzer/
│       │   └── ...
│       │
│       ├── database/                 ← Database design, queries, optimization
│       │   ├── schema-reviewer/
│       │   ├── query-optimizer/
│       │   └── ...
│       │
│       ├── devops/                   ← CI/CD, deployment, monitoring
│       │   ├── ci-cd-builder/
│       │   ├── deployment-preflight/
│       │   ├── monitoring-expert/
│       │   ├── chaos-engineer/
│       │   └── ...
│       │
│       ├── documentation/            ← Documentation generation
│       │   ├── readme-generator/
│       │   ├── adr-creator/
│       │   ├── api-documenter/
│       │   └── code-documenter/
│       │
│       ├── research/                 ← Investigation, spikes, mining
│       │   ├── codebase-explorer/
│       │   ├── tech-spike-planner/
│       │   ├── spec-miner/
│       │   └── deep-context-generator/
│       │
│       ├── project-management/       ← Planning, specs, features
│       │   ├── spec-writer/
│       │   ├── issue-creator/
│       │   ├── feature-forge/
│       │   ├── mvp-gatekeeper/
│       │   └── ...
│       │
│       ├── language/                 ← Language-specific experts
│       │   ├── dotnet-core-expert/
│       │   ├── csharp-developer/
│       │   ├── typescript-expert/
│       │   ├── python-expert/
│       │   └── ...
│       │
│       └── workflow/                 ← Internal workflow tools
│           ├── memory-optimization/
│           ├── token-optimization/
│           └── ...
│
├── 🔧 EXTENSIONS DIRECTORY (Custom tools and extensions)
│   └── extensions/
│       ├── custom-extension-1/
│       │   ├── manifest.json         ← Extension metadata
│       │   ├── index.js              ← Main extension file
│       │   ├── package.json          ← Dependencies (for npm extensions)
│       │   ├── README.md             ← Extension documentation
│       │   └── lib/                  ← Additional code
│       │
│       ├── custom-extension-2/
│       │   ├── manifest.json
│       │   ├── index.py              ← Python extension
│       │   ├── requirements.txt
│       │   └── ...
│       │
│       └── mcp-servers/              ← MCP (Model Context Protocol) servers
│           ├── mcp-filesystem/
│           ├── mcp-postgres/
│           ├── mcp-github/
│           └── ...
│
├── 🌐 BRIDGES DIRECTORY (Alternative location for model bridges)
│   └── bridges/                      ← Optional: Organize bridges separately
│       ├── CLAUDE.md
│       ├── GEMINI.md
│       ├── CODEX.md
│       └── README.md
│
├── ⚙️ CONFIG DIRECTORY (Project-specific configuration)
│   └── config/
│       ├── claude.config.json        ← IDE configuration (alternative)
│       ├── workspace.json            ← Workspace settings
│       ├── project-metadata.json     ← Project info (name, team, etc.)
│       ├── security.json             ← Security/compliance settings
│       └── integrations.json         ← External service configs
│
├── 📦 VENDOR/DEPENDENCIES (Third-party or shared)
│   └── vendor/                       ← Optional: Bundled third-party
│       ├── shared-skills/            ← Reusable skill library
│       ├── common-bridges/
│       └── ...
│
├── 📊 DATA DIRECTORY (Runtime data, caches)
│   └── .cache/                       ← Temporary data (git-ignored)
│       ├── skill-cache/
│       ├── manifest-cache/
│       └── ...
│
├── 📝 DOCUMENTATION (Detailed docs and guides)
│   └── docs/
│       ├── SETUP.md                  ← How to set up .claude
│       ├── USAGE.md                  ← How to use skills
│       ├── STRUCTURE.md              ← Custom structure documentation
│       ├── CONTRIBUTING.md           ← Contribution guidelines
│       ├── ARCHITECTURE.md           ← Design decisions
│       └── guides/
│           ├── getting-started.md
│           ├── skill-creation.md
│           └── ...
│
├── 🔐 SECURITY (Credentials, keys, sensitive)
│   └── secrets/                      ← GIT-IGNORED
│       ├── .env.local                ← Local environment variables
│       ├── .claude-credentials.json  ← API keys (git-ignored!)
│       ├── oauth-tokens/             ← Cached OAuth tokens
│       └── .gitignore                ← Ensure secrets are not committed
│
├── 🔄 SYNC/STATUS (Metadata for sync operations)
│   └── .sync/                        ← Git-ignored
│       ├── last-sync.json            ← Last sync timestamp
│       ├── sync-log.jsonl            ← Operation log
│       ├── conflicts.json            ← Unresolved conflicts
│       └── state.json                ← Sync state
│
├── 🗺️ STRUCTURE VARIANTS (For different project layouts)
│   └── layouts/                      ← Optional: Structure templates
│       ├── monorepo.json
│       ├── workspace.json
│       ├── enterprise.json
│       └── ...
│
├── 🔐 IGNORE FILES (Version control rules)
│   ├── .gitignore                    ← What NOT to commit
│   ├── .github-ignore                ← Custom ignore rules
│   └── .env.example                  ← Example env template (for sharing)
│
└── 📋 METADATA FILES
    ├── .claude-version               ← Schema version
    ├── .structure-version            ← Structure format version
    ├── LICENSE                       ← License for .claude contents
    └── CHANGELOG.md                  ← Changes over time
```

---

## Detailed Directory Breakdown

### 1. ROOT LEVEL MANIFEST FILES

| File | Required? | Purpose | Example Content |
|------|-----------|---------|-----------------|
| `manifest.json` | ✅ YES | Inventory of all skills, bridges, extensions | JSON with items array |
| `settings.json` | ⚠️ OPTIONAL | Claude IDE project settings | `{ "theme": "dark", "autoSave": true }` |
| `.structure.json` | ⚠️ OPTIONAL | Structure format metadata | `{ "version": "1.0", "layout": "nested" }` |
| `.claude-inherited` | ⚠️ OPTIONAL | Marker: inherits from parent (monorepo) | Empty file (presence = inheritance) |
| `README.md` | ⚠️ OPTIONAL | Custom .claude documentation | Markdown explaining project setup |
| `.claude-version` | ⚠️ OPTIONAL | Schema version tracking | Plain text: `1.0.0` |

### 2. MODEL BRIDGES (Root Level)

Each bridge file contains **instructions and context** for a specific LLM model.

| Bridge File | For | Purpose |
|---|---|---|
| `CLAUDE.md` | Claude 3.x, Claude Opus | Custom instructions, context, personality |
| `GEMINI.md` | Google Gemini | Model-specific guidance, context window limits |
| `CODEX.md` | GitHub Copilot Codex, Ollama | Code generation specific instructions |
| `OpenAI.md` | GPT-4, GPT-4o, gpt-3.5-turbo | OpenAI model specific context |
| `Anthropic.md` | Anthropic models | Model capabilities and constraints |
| `Google.md` | Google models (PaLM, etc.) | Google model specifics |
| `Local.md` | Local/self-hosted models | Instructions for local inference |

**Example CLAUDE.md structure:**
```markdown
---
model: claude-3.5-sonnet
version: "1.0.0"
context-window: 200000
capabilities: [reasoning, coding, analysis]
---

# Instructions for Claude

## Personality & Role
[Custom system instructions]

## Project Context
[Domain-specific knowledge]

## Tools Available
[List of skills, scripts, etc.]
```

### 3. SKILLS DIRECTORY

**Structure:** `skills/{category}/{skillName}/`

**Categories (12):**
1. **ai/** — Agent orchestration, LLM prompting, MCP servers
2. **security/** — OWASP, auth, encryption, threat modeling
3. **architecture/** — System design, patterns, code organization
4. **code-quality/** — Review, refactoring, debugging, metrics
5. **testing/** — Unit tests, TDD, coverage analysis
6. **database/** — Schema design, query optimization
7. **devops/** — CI/CD, deployment, monitoring, chaos
8. **documentation/** — READMEs, ADRs, API docs
9. **research/** — Exploration, spikes, specs
10. **project-management/** — Planning, specs, issues, features
11. **language/** — Language-specific experts (C#, Python, Go, etc.)
12. **workflow/** — Internal tooling, memory, optimization

**Each skill contains:**
```
skillName/
├── SKILL.md                  ← Main skill definition (REQUIRED)
│   • YAML frontmatter: name, description, version, metadata
│   • Core workflow steps
│   • Reference guide table
│
├── references/               ← Optional supporting docs
│   ├── topic-1.md
│   ├── topic-2.md
│   └── ...
│
├── README.md                 ← Optional: Skill overview
├── manifest.json             ← Optional: Skill metadata
├── examples/                 ← Optional: Usage examples
│   ├── example-1.md
│   └── example-2.md
│
└── lib/                      ← Optional: Code/scripts
    ├── utils.py
    ├── helpers.js
    └── ...
```

### 4. EXTENSIONS DIRECTORY

**Structure:** `extensions/{extensionName}/`

Custom extensions that extend Claude IDE functionality.

```
extensions/
├── my-custom-tool/
│   ├── manifest.json         ← Extension metadata
│   │   {
│   │     "name": "my-custom-tool",
│   │     "version": "1.0.0",
│   │     "type": "command|tool|service",
│   │     "main": "index.js"
│   │   }
│   │
│   ├── index.js              ← Main extension code
│   ├── package.json          ← NPM dependencies (if Node)
│   ├── requirements.txt      ← Python dependencies (if Python)
│   ├── README.md
│   ├── lib/                  ← Helper code
│   └── tests/                ← Extension tests
│
└── mcp-servers/              ← MCP (Model Context Protocol) servers
    ├── filesystem-mcp/       ← File system access via MCP
    ├── github-mcp/           ← GitHub API via MCP
    ├── postgres-mcp/         ← Database access via MCP
    └── custom-mcp/
```

### 5. BRIDGES DIRECTORY (Alternative Location)

Some projects organize bridges separately:

```
bridges/                       ← Optional: Separate from root
├── CLAUDE.md
├── GEMINI.md
├── CODEX.md
└── README.md
```

### 6. CONFIG DIRECTORY

Project-specific configuration (alternative to root-level settings.json):

```
config/
├── claude.config.json        ← Alternative to settings.json
├── workspace.json            ← Workspace configuration
│   {
│     "name": "EscrowApp",
│     "description": "Fintech escrow platform",
│     "theme": "dark"
│   }
│
├── project-metadata.json     ← Project information
│   {
│     "team": "platform-engineering",
│     "domain": "fintech",
│     "stack": ["dotnet", "blazor", "postgres"]
│   }
│
├── security.json             ← Security/compliance settings
│   {
│     "requireAuthorization": true,
│     "compliance": ["owasp", "pci-dss"],
│     "secretsRequired": ["api-keys", "db-credentials"]
│   }
│
└── integrations.json         ← External service connections
    {
      "github": { "org": "mycompany" },
      "slack": { "workspace": "engineering" },
      "stripe": { "mode": "live" }
    }
```

### 7. VENDOR DIRECTORY (Optional)

Bundled third-party or shared resources:

```
vendor/
├── shared-skills/            ← Reusable skill library
│   ├── logging-patterns/
│   ├── error-handling/
│   └── validation/
│
├── common-bridges/           ← Pre-configured bridges
│   ├── CLAUDE.md
│   └── GEMINI.md
│
└── templates/                ← Project templates
    ├── api-project/
    ├── web-app/
    └── library/
```

### 8. CACHE DIRECTORY (Git-Ignored)

Runtime data and caches:

```
.cache/                       ← GIT-IGNORED: /.cache
├── skill-cache/              ← Compiled/cached skills
├── manifest-cache/           ← Parsed manifest cache
├── symbol-index/             ← Code symbol cache
└── dependency-graph/         ← Resolved dependencies
```

### 9. DOCUMENTATION DIRECTORY

Detailed guides and documentation:

```
docs/
├── SETUP.md                  ← Initial setup instructions
├── USAGE.md                  ← How to use .claude
├── STRUCTURE.md              ← Custom structure explanation
├── CONTRIBUTING.md           ← Contribution guidelines
├── ARCHITECTURE.md           ← Design decisions
├── ROADMAP.md                ← Future plans
│
├── guides/
│   ├── getting-started.md
│   ├── skill-creation.md
│   ├── extension-development.md
│   ├── adding-bridges.md
│   └── troubleshooting.md
│
├── tutorials/
│   ├── first-skill.md
│   ├── custom-extension.md
│   └── workspace-setup.md
│
└── api/                      ← API documentation (if applicable)
    ├── manifest-schema.md
    ├── settings-schema.md
    └── extension-api.md
```

### 10. SECRETS DIRECTORY (Git-Ignored)

Sensitive credentials and keys:

```
secrets/                       ← GIT-IGNORED: /secrets
├── .env.local                ← Local environment variables
│   CLAUDE_API_KEY=sk-...
│   STRIPE_SECRET=rk_live_...
│   DB_PASSWORD=...
│
├── .claude-credentials.json  ← API credentials (GIT-IGNORED)
│   {
│     "github-token": "ghp_...",
│     "stripe-api": "sk_...",
│     "openai-key": "sk-..."
│   }
│
├── oauth-tokens/             ← Cached OAuth tokens
│   ├── github-token.json
│   └── slack-token.json
│
└── .gitignore                ← Ensure secrets not committed
    secrets/
    .env.local
    .claude-credentials.json
```

### 11. SYNC/STATUS DIRECTORY (Git-Ignored)

Metadata for sync operations:

```
.sync/                         ← GIT-IGNORED: /.sync
├── last-sync.json            ← Last sync timestamp
│   { "timestamp": "2026-04-15T18:37:34Z", "source": "global" }
│
├── sync-log.jsonl            ← Operation log (newline-delimited)
│   {"action": "copy", "item": "claude-export", "status": "ok"}
│   {"action": "update", "item": "CLAUDE.md", "status": "ok"}
│
├── conflicts.json            ← Unresolved conflicts
│   { "items": ["CLAUDE.md"], "reason": "version-mismatch" }
│
└── state.json                ← Sync state
    { "synced": true, "lastCheck": "...", "nextCheck": "..." }
```

### 12. LAYOUTS DIRECTORY (Optional)

Structure templates for different project types:

```
layouts/                       ← Optional: Structure templates
├── monorepo.json             ← Monorepo structure config
├── workspace.json            ← Multi-workspace config
├── enterprise.json           ← Enterprise/team structure
├── microservices.json        ← Microservices architecture
└── single-project.json       ← Single project config
```

---

## File Type Summary

| File Type | Purpose | Examples |
|-----------|---------|----------|
| `.md` | Documentation, instructions | SKILL.md, CLAUDE.md, README.md |
| `.json` | Configuration, metadata, manifests | manifest.json, settings.json, config files |
| `.jsonl` | Line-delimited JSON (logs) | sync-log.jsonl |
| `.js` | JavaScript extensions, tools | Extension code, MCP servers |
| `.py` | Python skills, extensions | Python-based tools |
| `.ts` / `.tsx` | TypeScript extensions | Modern JS extensions |
| `.sh` / `.ps1` | Shell scripts | PowerShell, bash automation |
| `.yaml` / `.yml` | Configuration | Alternative to JSON config |
| `.env` | Environment variables | Secrets, local config (git-ignored) |
| Empty marker files | Signaling | `.claude-inherited`, `.structure-version` |

---

## Naming Conventions

### Skills
- Directory: `{skillName}` (kebab-case)
- Main file: `SKILL.md` (always)
- References: `{topic-name}.md` (kebab-case, descriptive)
- Example: `agent-orchestrator/`, `SKILL.md`, `references/delegation-patterns.md`

### Bridges (Model Instructions)
- Format: `{MODEL}.md` (UPPERCASE)
- Examples: `CLAUDE.md`, `GEMINI.md`, `CODEX.md`, `OpenAI.md`

### Extensions
- Directory: `{extensionName}` (kebab-case)
- Main file: `index.js`, `index.py`, or `index.ts`
- Manifest: `manifest.json`
- Example: `my-custom-tool/`, `index.js`, `manifest.json`

### Configuration
- Root: `settings.json`, `manifest.json`
- Subdirectories: `config/{purpose}.json` or `config/{purpose}.yaml`
- Example: `config/security.json`, `config/integrations.json`

---

## Required vs Optional

### REQUIRED (Skill must have)
- ✅ `skills/{category}/{skillName}/SKILL.md` — Main skill file with YAML frontmatter

### REQUIRED (Project must have)
- ✅ `manifest.json` — Inventory of skills/bridges/extensions

### RECOMMENDED
- ⚠️ Model bridges: `CLAUDE.md`, `GEMINI.md`
- ⚠️ `settings.json` — Project settings
- ⚠️ `README.md` — Setup instructions

### OPTIONAL
- ❌ `references/` in skills — Supporting docs
- ❌ `extensions/` — Custom tools
- ❌ `config/` — Additional configuration
- ❌ `docs/` — Documentation
- ❌ `vendor/` — Third-party code
- ❌ All git-ignored directories (`.cache/`, `secrets/`, `.sync/`)

---

## Git Ignore Rules

```gitignore
# ALWAYS ignore secrets and runtime data
secrets/
.env.local
.claude-credentials.json
oauth-tokens/
.cache/
.sync/

# OPTIONAL: Ignore all .claude (local IDE setup only)
# .claude/

# OPTIONAL: Commit .claude (shared team config)
# !.claude/
# !.claude/**
```

---

## Size Guidelines

| Component | Typical Size | Notes |
|-----------|--------------|-------|
| Single skill | 5-50 KB | SKILL.md + 1-5 references |
| skill with large refs | 50-200 KB | Comprehensive skill with many examples |
| Bridge file | 5-20 KB | Model instructions |
| Extension | 10-100 KB | Depends on complexity |
| Complete `.claude` | 500 KB - 10 MB | Depends on number of skills/extensions |
| Project-specific override | 50-200 KB | Per-package skills in monorepo |

---

## Example: Full EscrowApp Structure

```
EscrowApp/.claude/
├── manifest.json
├── settings.json
├── CLAUDE.md
├── GEMINI.md
├── CODEX.md
├── OpenAI.md
│
├── skills/
│   ├── ai/
│   │   ├── agent-orchestrator/
│   │   ├── claude-export/
│   │   ├── mcp-developer/
│   │   └── prompt-engineer/
│   │
│   ├── security/
│   │   ├── owasp-audit/
│   │   ├── authentication/
│   │   ├── authorization/
│   │   ├── secret-scanner/
│   │   └── threat-modeler/
│   │
│   ├── architecture/
│   │   ├── architecture-reviewer/
│   │   ├── design-pattern-advisor/
│   │   └── dependency-analyzer/
│   │
│   ├── code-quality/
│   │   ├── code-reviewer/
│   │   ├── refactor-planner/
│   │   ├── quality-analyzer/
│   │   └── tech-debt-tracker/
│   │
│   ├── testing/
│   │   ├── test-generator/
│   │   ├── test-coverage-analyzer/
│   │   └── tdd-coach/
│   │
│   ├── database/
│   │   ├── schema-reviewer/
│   │   └── query-optimizer/
│   │
│   ├── devops/
│   │   ├── ci-cd-builder/
│   │   ├── deployment-preflight/
│   │   └── monitoring-expert/
│   │
│   ├── documentation/
│   │   ├── readme-generator/
│   │   ├── api-documenter/
│   │   └── adr-creator/
│   │
│   ├── language/
│   │   ├── dotnet-core-expert/
│   │   └── csharp-developer/
│   │
│   └── workflow/
│       └── memory-optimization/
│
├── extensions/
│   ├── stripe-mcp/
│   │   ├── manifest.json
│   │   ├── index.js
│   │   └── lib/
│   │
│   └── github-mcp/
│       ├── manifest.json
│       ├── index.js
│       └── lib/
│
├── config/
│   ├── claude.config.json
│   ├── security.json
│   └── integrations.json
│
├── docs/
│   ├── SETUP.md
│   ├── USAGE.md
│   └── guides/
│
├── secrets/ (git-ignored)
│   ├── .env.local
│   └── .claude-credentials.json
│
└── .gitignore
```

