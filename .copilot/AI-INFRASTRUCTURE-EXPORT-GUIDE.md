# AI Infrastructure Export Guide

> **Master reference for replicating this project's AI infrastructure to another project.**
> Read this document FIRST. It tells you what to copy, what to customize, and what to rewrite.

---

## 1. Executive Summary

### What This AI Infrastructure Provides

This project implements **multi-model AI orchestration** — a unified infrastructure that enables consistent, high-quality AI-assisted development across four major AI coding assistants simultaneously:

- **GitHub Copilot CLI** — Custom extensions, workflow skills, and project-scoped instructions
- **Claude Code** — Rules, hooks, bridge skills, and lifecycle automation
- **Google Gemini / Antigravity** — Native rules, workflows, and universal skill library
- **OpenAI Codex CLI** — Hierarchical instructions, safety guardrails, and autonomous execution config

### Why It Exists

AI coding assistants are only as effective as the context they receive. Without shared infrastructure:

- Each tool gets different instructions, producing inconsistent code
- Architectural decisions drift across sessions and models
- Security rules, naming conventions, and patterns are applied unevenly
- Onboarding a new AI tool means starting from scratch

This infrastructure solves that by providing a **single source of truth** for project knowledge, coding standards, and workflow automation — consumed by all three AI models through their native configuration mechanisms.

### What You Get

| Asset | Count | Description |
|-------|-------|-------------|
| **Universal Skills** | 43 skills across 12 categories | Reusable methodology library (code review, security audit, TDD, architecture analysis, etc.) |
| **Copilot CLI Extensions** | 7 extensions | Custom tools: superpowers, build-guardian, security-scanner, context-optimizer, dotnet-conventions, research-first, doc-sync |
| **Claude Code Rules** | 10 rule files | Auto-loaded contextual rules for clean architecture, OWASP, EF Core, Blazor, Polly, DDD, CQRS, MVP, memory optimization, testing |
| **Claude Code Hooks** | 10 hook scripts | Lifecycle automation: build checks, security scans, doc sync, convention enforcement |
| **Antigravity Rules** | 11 rule files | Native `.agent/rules/` for Gemini/Antigravity — adapted from Claude rules |
| **Antigravity Workflows** | 4 workflow files | Multi-step automation: new-feature, security-review, build-and-test, new-component |
| **Codex CLI Config** | config.toml + 5 subdirectory AGENTS.md | Approval mode, safety guardrails, hierarchical layer-specific instructions |
| **Instruction Files** | 5 files | AGENTS.md (canonical), CLAUDE.md, GEMINI.md, CODEX.md, copilot-instructions.md |

---

## 2. AI Infrastructure Inventory

### Complete Inventory

| Layer | Location | Count | Purpose |
|-------|----------|-------|---------|
| Skills (Universal) | `.github/skills/` | 43 skills across 12 categories | Domain-agnostic methodology library — works with any AI model that can read files |
| Skills (Claude Bridges) | `.claude/skills/` | 43 bridge files | Claude Code `/skill` registration — thin redirects to `.github/skills/` |
| Extensions (Copilot CLI) | `.github/extensions/` | 7 extensions | Custom tools exposed to Copilot CLI: superpowers, build-guardian, security-scanner, context-optimizer, dotnet-conventions, research-first, doc-sync |
| Rules (Claude Code) | `.claude/rules/` | 10 rule files | Auto-loaded contextual rules: clean-arch, OWASP, EF Core, Blazor, Polly, DDD, CQRS, MVP, memory, testing |
| Rules (Antigravity) | `.agent/rules/` | 11 rule files | Antigravity-native rules: GEMINI.md entry point + 10 domain rules (adapted from Claude rules) |
| Workflows (Antigravity) | `.agent/workflows/` | 4 workflow files | Multi-step automation: new-feature, security-review, build-and-test, new-component |
| Hooks (Claude Code) | `.claude/hooks/` | 10 hook scripts | Lifecycle automation: build checks, security scans, doc sync, conventions |
| Instruction Files | Root + `.github/` | 5 files | AGENTS.md (canonical), CLAUDE.md, GEMINI.md, CODEX.md, copilot-instructions.md |
| Subdirectory Instructions | `EscrowApp/{layer}/` | 5 AGENTS.md files | Codex CLI hierarchical context: Features/, Models/, Components/, Data/, Services/ |
| Settings (Claude) | `.claude/` | 2 JSON files + .claudeignore | settings.json (hooks config), settings.local.json (permissions), .claudeignore (exclusions) |
| Settings (Gemini) | `.gemini/` | 1 JSON file | settings.json — repo-level Gemini configuration |
| Config (Codex CLI) | `.codex/` | 2 files | config.toml (model, approval, safety), README.md |

### Skills Breakdown by Category (43 total)

| Category | Skills | Path |
|----------|--------|------|
| Code Quality (7) | code-reviewer, refactor-planner, code-documenter, debugging-wizard, quality-analyzer, smart-refactor, tech-debt-tracker | `.github/skills/code-quality/` |
| Security (5) | owasp-audit, secret-scanner, threat-modeler, authentication, authorization | `.github/skills/security/` |
| Architecture (5) | architecture-reviewer, design-pattern-advisor, dependency-analyzer, legacy-modernizer, polyglot-analyzer | `.github/skills/architecture/` |
| Testing (3) | test-generator, tdd-coach, test-coverage-analyzer | `.github/skills/testing/` |
| Database (2) | schema-reviewer, query-optimizer | `.github/skills/database/` |
| DevOps (4) | ci-cd-builder, deployment-preflight, monitoring-expert, chaos-engineer | `.github/skills/devops/` |
| Documentation (3) | readme-generator, adr-creator, api-documenter | `.github/skills/documentation/` |
| Research (3) | codebase-explorer, tech-spike-planner, spec-miner, deep-context-generator | `.github/skills/research/` |
| Project Management (4) | spec-writer, issue-creator, feature-forge, mvp-gatekeeper | `.github/skills/project-management/` |
| AI (3) | mcp-developer, prompt-engineer, agent-orchestrator | `.github/skills/ai/` |
| Language (2) | dotnet-core-expert, csharp-developer | `.github/skills/language/` |
| Workflow (1) | memory-optimization | `.github/skills/workflow/` |

### Extensions Breakdown (7 total)

| Extension | Purpose | Portability |
|-----------|---------|-------------|
| **superpowers** | Workflow skill loader (brainstorming, TDD, debugging, planning, verification) | ✅ Copy as-is |
| **build-guardian** | Runs `dotnet build` and `dotnet test`, reports structured results | 🔴 Rewrite for your build system |
| **context-optimizer** | Returns project summary, checks docs, validates planning status | 🔴 Rewrite project_summary() content |
| **security-scanner** | OWASP scan, secret detection, convention checks | 🟡 Customize scan patterns |
| **dotnet-conventions** | Checks file-scoped namespaces, code-behind, naming, inline styles | 🟡 Customize for your conventions |
| **research-first** | Reminds to check docs before coding, validates implementation intent | 🟡 Update intent keywords |
| **doc-sync** | Compares source timestamps vs doc timestamps, flags stale docs | 🟡 Remap feature/doc directory structure |

### Instruction Files Breakdown (5 total)

| File | Audience | Content |
|------|----------|---------|
| **AGENTS.md** | All AI models | Canonical source of truth — architecture, patterns, conventions, security, regulatory, skills catalog |
| **CLAUDE.md** | Claude Code | Claude-specific reasoning guidance, chain-of-thought templates, security review methodology |
| **GEMINI.md** | Google Gemini / Antigravity | Gemini-specific exploration strategy, dependency mapping, cross-referencing guidance |
| **CODEX.md** | OpenAI Codex CLI | Autonomous execution guardrails, fintech safety rules, tool restriction policies |
| **copilot-instructions.md** | GitHub Copilot (all surfaces) | Copilot-specific rules loaded via `.github/copilot-instructions.md` |

---

## 3. Portability Assessment

### ✅ 100% Portable — Copy As-Is

These assets require **zero changes** to work in another project.

#### Universal Skills (38 of 43)

All domain-agnostic skills copy directly. Each skill is a self-contained methodology in markdown — no project-specific references.

**Code Quality (7):**
- `code-reviewer` — Systematic code review covering SOLID, security, performance, testability
- `refactor-planner` — Safe incremental refactoring with dependency mapping and blast radius analysis
- `code-documenter` — Generate XML doc comments, JSDoc/TSDoc, inline comments, README sections
- `debugging-wizard` — Reproduce → isolate → hypothesize → fix → prevent methodology
- `quality-analyzer` — Cyclomatic/cognitive complexity, maintainability index, SATD detection
- `smart-refactor` — Metrics-driven refactoring with baseline/after comparison
- `tech-debt-tracker` — Detect, quantify, and prioritize technical debt

**Security (3 of 5):**
- `owasp-audit` — Full OWASP Top 10 security audit with severity ratings and remediation
- `secret-scanner` — Detect exposed secrets, API keys, tokens, and credentials
- `threat-modeler` — STRIDE-based threat modeling with DREAD scoring

**Architecture (5):**
- `architecture-reviewer` — Clean Architecture compliance, SOLID principles, dependency direction
- `design-pattern-advisor` — Pattern recommendations for code smells and architectural problems
- `dependency-analyzer` — Vulnerability scanning, outdated packages, license risks
- `legacy-modernizer` — Strangler fig, branch by abstraction, feature flags migration strategies
- `polyglot-analyzer` — Multi-language quality comparison and cross-language boundary analysis

**Testing (3):**
- `test-generator` — Comprehensive unit/integration test generation with edge cases and mocks
- `tdd-coach` — Red-Green-Refactor TDD cycle guidance
- `test-coverage-analyzer` — Coverage gap detection, risk prioritization, test smell identification

**Database (1 of 2):**
- `schema-reviewer` — Normalization, indexing, naming, and constraint review

**DevOps (4):**
- `ci-cd-builder` — Multi-stage CI/CD pipelines with caching, testing, and deployment
- `deployment-preflight` — Pre-deployment verification (build, tests, migrations, security, rollback)
- `monitoring-expert` — Prometheus/Grafana dashboards, alerting rules, OpenTelemetry tracing
- `chaos-engineer` — Failure injection frameworks, game day exercises, runbooks

**Documentation (3):**
- `readme-generator` — Comprehensive README.md from project analysis
- `adr-creator` — Architecture Decision Records in MADR format
- `api-documenter` — API documentation with endpoint inventory and OpenAPI specs

**Research (4):**
- `codebase-explorer` — Deep codebase analysis producing architecture maps and dependency graphs
- `tech-spike-planner` — Time-boxed technical investigations with clear acceptance criteria
- `spec-miner` — Reverse-engineering specifications from existing codebases
- `deep-context-generator` — LLM-optimized codebase context for onboarding and analysis

**Project Management (4):**
- `spec-writer` — Comprehensive technical specifications from feature requests
- `issue-creator` — Structured GitHub issues with acceptance criteria and sub-task decomposition
- `feature-forge` — Requirements workshops producing EARS-format feature specs
- `mvp-gatekeeper` — MVP scope discipline enforcement

**AI (3):**
- `mcp-developer` — MCP server/client development, tool handlers, transport layers
- `prompt-engineer` — Prompt optimization, structured output schemas, evaluation rubrics
- `agent-orchestrator` — Parallel sub-agent fleets with DAG-based dependency management

**Workflow (1):**
- `memory-optimization` — Context window and token optimization rules

#### .NET-Specific Skills (5 of 43)

Portable to **other .NET projects** without changes. Not applicable to non-.NET stacks.

- `authentication` — ASP.NET Core + Blazor authentication with Entra ID, Identity, OIDC, JWT
- `authorization` — Policy-based, resource-based, claims-based authorization patterns
- `query-optimizer` — EF Core LINQ, raw SQL, and Dapper query anti-pattern detection
- `dotnet-core-expert` — .NET 10 expertise: minimal APIs, Clean Architecture, EF Core, CQRS
- `csharp-developer` — C# 13 mastery: records, pattern matching, primary constructors

#### Claude Bridge Skills (43 of 43)

All bridge files in `.claude/skills/` are thin redirects. They contain no project-specific logic — just skill name registration and a path to the universal skill file. **Copy the entire folder.**

#### Other Fully Portable Assets

| Asset | Why It's Portable |
|-------|-------------------|
| `.github/extensions/superpowers/` | Generic workflow skills (brainstorming, TDD, debugging, planning) — no project references |
| `.claude/settings.local.json` | Permission declarations only — no project-specific content |
| `.claudeignore` | Standard exclusion patterns (bin/, obj/, .vs/, node_modules/) — works for any .NET project |
| `.github/skills/CATALOG.md` | Skill discovery index — auto-references skill paths |
| `.github/skills/README.md` | Usage instructions for the skill system |
| `.github/AI-COMPATIBILITY-AUDIT.md` | Audit methodology template — replace tool-specific findings with your project's results |

---

### 🟡 Portable with Minor Edits (~80% Reusable)

These assets have a generic structure with project-specific values embedded. Copy the file, then find-and-replace or edit the marked sections.

#### Extensions (4 of 7)

**`dotnet-conventions`**
- What to change: Target file patterns (e.g., `.razor` → `.vue`), naming rules, convention checks
- Effort: ~30 minutes
- Look for: `glob` patterns, file extension checks, naming regex patterns

**`context-optimizer`**
- What to change: The `project_summary()` function content — architecture description, layer map, tech stack
- Effort: ~1 hour
- Look for: The main summary string/template that describes your project

**`research-first`**
- What to change: Implementation-intent keywords that trigger "check docs first" reminders
- Effort: ~15 minutes
- Look for: Keyword arrays or pattern-match lists

**`doc-sync`**
- What to change: The feature-to-doc directory mapping configuration
- Effort: ~30 minutes
- Look for: `doc-sync.config.json` or equivalent mapping file

#### Claude Rules (4 of 10)

**`ef-core.md`**
- What to change: Database provider (PostgreSQL → SQL Server/MySQL), type mappings (`numeric(18,4)` → `decimal(18,4)`), migration commands
- Effort: ~20 minutes

**`owasp-security.md`**
- What to change: Remove fintech-specific sections (PCI-DSS compliance, idempotency key requirements, Stripe-specific patterns) for non-payment projects
- Effort: ~30 minutes

**`polly-resilience.md`**
- What to change: Retry counts, timeout values, target API names (Stripe → your external API), circuit breaker thresholds
- Effort: ~20 minutes

**`testing-standards.md`**
- What to change: Test framework references if not using xUnit, container strategy if not using Testcontainers/PostgreSQL, builder pattern examples
- Effort: ~30 minutes

#### Claude Hooks (6 of 10)

| Hook Script | What to Change | Effort |
|-------------|----------------|--------|
| `security-scanner.ps1` | Add/remove secret patterns for your domain (e.g., AWS keys instead of Stripe keys) | ~20 min |
| `dotnet-conventions.ps1` | Adjust naming rules, pattern checks, file extension targets | ~20 min |
| `doc-sync-reminder.ps1` | Remap feature directories to match your project's doc structure | ~15 min |
| `build-reminder.ps1` | Change file extension triggers (e.g., `.py` instead of `.cs`) | ~10 min |
| `notification.ps1` | Update notification channels, webhook URLs, or alerting config | ~10 min |
| `test-runner.ps1` | Update test project paths, test runner commands, coverage thresholds | ~15 min |

#### Antigravity Rules (8 of 11)

All rules adapted from Claude's `.claude/rules/` — same domain knowledge, Antigravity-native format.

**Copy as-is (for .NET projects):**
- `clean-architecture.md`, `cqrs-mediatr.md`, `blazor-components.md`, `mvp-first.md`, `memory-optimization.md`, `GEMINI.md` (entry point)

**Customize:**
- `ef-core.md` — Update DB provider/type mappings
- `owasp-security.md` — Remove fintech-specific sections

**Rewrite:**
- `ddd-domain.md` — Your aggregate roots, state machines, domain events
- `polly-resilience.md` — Your external API names, retry/timeout values
- `testing-standards.md` — Your test framework references

#### Antigravity Workflows (4 of 4)

All workflows are generic development patterns. **Copy as-is** for .NET projects:
- `new-feature.md` — MediatR vertical slice creation
- `security-review.md` — OWASP audit workflow
- `build-and-test.md` — Standard build/test cycle
- `new-component.md` — Blazor component creation

For non-.NET: rewrite `new-feature.md` and `new-component.md` for your framework; keep `security-review.md` and `build-and-test.md` with command substitution.

#### Gemini Settings

**`.gemini/settings.json`** — 🟡 Customize project name, type, framework, and exclude patterns.

#### Codex CLI Config

**`.codex/config.toml`** — 🟡 Customize:
- `[model]` — default model name
- `[project]` — project name, type, framework, description
- `[safety]` — protected paths for your project structure
- `[context]` — fallback filenames if you use CODEX.md pattern

**`.codex/README.md`** — 🟡 Update directory description for your project.

#### Codex Subdirectory AGENTS.md Files (5 files)

These provide layer-specific context when Codex works in a directory. **Must be rewritten** for your project's layer structure:

| File | Content to Rewrite |
|------|-------------------|
| `Features/AGENTS.md` | Your feature/handler patterns |
| `Models/AGENTS.md` | Your domain entities and invariants |
| `Components/AGENTS.md` | Your UI framework patterns |
| `Data/AGENTS.md` | Your database/ORM patterns |
| `Services/AGENTS.md` | Your external service integration patterns |

Keep files under 800 bytes each — Codex has a 32KB instruction stack cap.

#### Instruction Files — Generic Sections

These sections from the instruction files are reusable as-is or with minor template substitution:

**From AGENTS.md:**
- Architecture Overview section → ✅ reusable as a **template** (replace layer names and directories)
- Design Patterns table → ✅ reusable as a **template** (replace pattern applications)
- Code Conventions → ✅ copy as-is for any C# project
- Skills Catalog → ✅ reusable as a **template** (paths are relative, work anywhere)

**From CLAUDE.md:**
- Reasoning Approach → ✅ copy as-is (generic SOLID/architecture reasoning checklist)
- Security Review Methodology → ✅ copy as-is (OWASP Top 10 evaluation table)
- Immutability Preferences → ✅ copy as-is for any C# project
- C# Code Generation Rules → ✅ copy as-is for any C# project

**From GEMINI.md:**
- Exploration Strategy → ✅ copy as-is (generic dependency-first investigation approach)
- Code Style Rules → ✅ copy as-is for any C# project
- Feature Modification Workflow → ✅ reusable as a **template** (replace directory paths)

**From copilot-instructions.md:**
- Dependency Direction diagram → ✅ reusable as a **template**
- OWASP Security section → ✅ copy as-is
- Code Conventions → ✅ copy as-is for any C# project
- Blazor Rules → ✅ copy as-is for any Blazor project

---

### 🔴 Must Rewrite — Project-Specific Content

These assets are tightly coupled to this project's domain, build system, or architecture. They serve as **reference examples** but must be rewritten for your project.

#### Extensions (2 of 7)

**`build-guardian`**
- Why: Hardcoded to `EscrowApp.sln`, uses `dotnet build` and `dotnet test` commands with project-specific paths
- What to do: Rewrite for your solution file, build tool (npm/pip/cargo/go), and test runner
- Reference value: The extension structure and error-reporting pattern are reusable

**`security-scanner`**
- Why: Contains project-specific scan patterns (Stripe API key formats, fintech-specific terms, EscrowApp namespace references)
- What to do: Replace scan patterns with your domain's sensitive patterns (AWS keys, database credentials, etc.)
- Reference value: The OWASP scanning methodology and output format are reusable

#### Claude Rules (1 of 10)

**`ddd-domain.md`**
- Why: References `EscrowTransaction` as the aggregate root, defines project-specific state machine (Pending → Held → Released | Disputed), lists project-specific domain events and value objects
- What to do: Rewrite with your aggregate roots, state machines, domain events, and value objects
- Reference value: The DDD principles and patterns described are universal — only the examples need replacement

#### Claude Hooks (4 of 10)

| Hook Script | Why It Must Be Rewritten |
|-------------|--------------------------|
| `context-optimizer.ps1` | Injects EscrowApp-specific architecture context, layer descriptions, and tech stack summary |
| `research-first.ps1` | Contains project-specific intent keywords for the "check docs before coding" workflow |
| `doc-sync-reminder.ps1` | Maps to EscrowApp's specific feature directory ↔ documentation file relationships |
| `build-reminder.ps1` | References `.cs`, `.razor`, `.csproj` file extensions — rewrite for your stack's extensions |

#### Instruction Files — Project-Specific Sections

These sections **must be rewritten** for every new project:

**From AGENTS.md:**
- Project Identity (name, domain, users, tech stack, target)
- CQRS & MediatR slice table (your use cases, not escrow use cases)
- Data Model (your entities, not EscrowTransaction)
- Fintech Guardrails (your domain's business rules)
- Documentation structure (your docs/ layout)
- DI Registration reference (your Program.cs registrations)

**From CLAUDE.md:**
- Project Context section
- MediatR Handler Design example ("RefundFunds" → your domain operation)
- Documentation Updates mapping table (your features → your docs)
- Domain Model Reference (your entities and relationships)
- Error Handling Guidance (your domain exceptions)

**From GEMINI.md:**
- Project Context section
- Cross-Referencing Checklist (your directories and key files)
- Existing Component Reference table
- Program.cs Service Registration Reference
- Database & EF Core Guidance (your schema, your migrations)

**From copilot-instructions.md:**
- Project description and tech stack
- Data Model definition
- Payment Rules / Domain Rules (your business invariants)
- CQRS Flow and Existing Slices table
- DI Registration patterns in Program.cs
- Localization resource paths

---

## 4. The Fastest Path

### For a New .NET Project (~70% Copy-Paste)

> **Estimated total effort: 2–3 hours** to fully customize for a new .NET project.

#### Step 1: Copy Universal Skills ✅

```powershell
# Copy the entire skills library — zero changes needed
Copy-Item -Recurse ".github/skills/" "$NewProject/.github/skills/"
```

All 43 skills work immediately. The 5 .NET-specific skills (authentication, authorization, query-optimizer, dotnet-core-expert, csharp-developer) are directly applicable.

#### Step 2: Copy Claude Bridge Skills ✅

```powershell
# Copy all bridge files — they just redirect to .github/skills/
Copy-Item -Recurse ".claude/skills/" "$NewProject/.claude/skills/"
```

#### Step 3: Copy Portable Extensions ✅

```powershell
# Superpowers works as-is
Copy-Item -Recurse ".github/extensions/superpowers/" "$NewProject/.github/extensions/superpowers/"
```

#### Step 4: Copy and Customize Other Extensions 🟡

```powershell
# Copy all extensions
Copy-Item -Recurse ".github/extensions/" "$NewProject/.github/extensions/"
```

Then edit:
- `build-guardian/` — Update solution file path, build/test commands
- `context-optimizer/` — Rewrite `project_summary()` for your architecture
- `security-scanner/` — Update secret patterns for your domain
- `dotnet-conventions/` — Adjust convention rules for your coding standards
- `research-first/` — Update intent keywords
- `doc-sync/` — Remap feature-to-doc directory structure

#### Step 5: Copy and Customize Claude Rules 🟡

```powershell
Copy-Item -Recurse ".claude/rules/" "$NewProject/.claude/rules/"
```

Edit these files:
- `ddd-domain.md` — Replace aggregate roots, state machines, domain events with yours
- `ef-core.md` — Update DB provider and type mappings if different
- `owasp-security.md` — Remove fintech-specific sections if not a payment platform
- `polly-resilience.md` — Update API names, retry counts, timeout values
- `testing-standards.md` — Adjust framework/container references if different

Keep as-is:
- `clean-architecture.md`, `cqrs-mediatr.md`, `blazor-components.md`, `mvp-first.md`, `memory-optimization.md`

#### Step 6: Copy and Customize Claude Hooks 🟡

```powershell
Copy-Item -Recurse ".claude/hooks/" "$NewProject/.claude/hooks/"
```

Update paths and patterns in each hook script (see the hooks table in Section 3).

#### Step 7: Copy and Customize Claude Settings 🟡

```powershell
Copy-Item ".claude/settings.json" "$NewProject/.claude/settings.json"
Copy-Item ".claude/settings.local.json" "$NewProject/.claude/settings.local.json"  # ✅ as-is
```

Update `settings.json`:
- Hook file paths (if directory structure differs)
- API credential references (if hooks call external services)

#### Step 8: Create Instruction Files 🔴

Use the existing files as **templates**. For each file:

1. Keep all generic sections (marked ✅ in Section 3)
2. Replace all project-specific sections (marked 🔴 in Section 3) with your project's details
3. Update the architecture diagram, layer map, and dependency direction for your project

**Recommended approach:**
1. Copy each file to your new project
2. Search for "Escrow", "NexTruzt", "Stripe", "fintech" — these mark project-specific content
3. Replace with your project's domain terminology, entities, and patterns

#### Step 9: Copy and Customize Codex CLI Config 🟡

```powershell
Copy-Item -Recurse ".codex/" "$NewProject/.codex/"
Copy-Item "CODEX.md" "$NewProject/CODEX.md"
```

Edit:
- `.codex/config.toml` — Update project name, type, framework, protected paths
- `CODEX.md` — Replace all project-specific sections (search for "Escrow", "NexTruzt", "Stripe")

#### Step 10: Copy and Customize Antigravity Config 🟡

```powershell
Copy-Item -Recurse ".agent/" "$NewProject/.agent/"
Copy-Item -Recurse ".gemini/" "$NewProject/.gemini/"
```

Edit:
- `.agent/rules/ddd-domain.md` — Rewrite for your domain model
- `.agent/rules/GEMINI.md` — Update compliance warning for your domain
- `.gemini/settings.json` — Update project name, type, framework

Keep as-is: `clean-architecture.md`, `cqrs-mediatr.md`, `blazor-components.md`, `mvp-first.md`, `memory-optimization.md`

#### Step 11: Create Subdirectory AGENTS.md Files 🔴

For Codex CLI hierarchical context, create concise AGENTS.md files (~500 bytes each) in key source directories:

```powershell
# Create one per architectural layer
New-Item -ItemType File "$NewProject/src/Features/AGENTS.md"
New-Item -ItemType File "$NewProject/src/Models/AGENTS.md"
New-Item -ItemType File "$NewProject/src/Components/AGENTS.md"
New-Item -ItemType File "$NewProject/src/Data/AGENTS.md"
New-Item -ItemType File "$NewProject/src/Services/AGENTS.md"
```

Use the existing EscrowApp subdirectory AGENTS.md files as templates — replace domain-specific content with your project's patterns.

#### Step 12: Verify

```powershell
# Verify skills are discoverable
Get-ChildItem "$NewProject/.github/skills/*/SKILL.md" | Measure-Object

# Verify extensions load (in Copilot CLI)
# /extensions list

# Verify Claude rules are present
Get-ChildItem "$NewProject/.claude/rules/*.md" | Measure-Object

# Verify Antigravity rules are present
Get-ChildItem "$NewProject/.agent/rules/*.md" | Measure-Object

# Verify Antigravity workflows are present
Get-ChildItem "$NewProject/.agent/workflows/*.md" | Measure-Object

# Verify Codex config exists
Test-Path "$NewProject/.codex/config.toml"
Test-Path "$NewProject/CODEX.md"

# Verify Gemini settings exist
Test-Path "$NewProject/.gemini/settings.json"

# Verify build works
dotnet build
```

---

### For a Non-.NET Project (Node/Python/Go) (~40% Copy-Paste)

> **Estimated total effort: 4–6 hours** to fully customize for a non-.NET stack.

#### Step 1: Copy Universal Skills (38 of 43) ✅

```powershell
Copy-Item -Recurse ".github/skills/" "$NewProject/.github/skills/"
```

Then **remove** the 5 .NET-specific skills:
```powershell
Remove-Item -Recurse "$NewProject/.github/skills/security/authentication/"
Remove-Item -Recurse "$NewProject/.github/skills/security/authorization/"
Remove-Item -Recurse "$NewProject/.github/skills/database/query-optimizer/"
Remove-Item -Recurse "$NewProject/.github/skills/language/dotnet-core-expert/"
Remove-Item -Recurse "$NewProject/.github/skills/language/csharp-developer/"
```

Update `CATALOG.md` to remove references to deleted skills.

#### Step 2: Copy Superpowers Extension ✅

```powershell
Copy-Item -Recurse ".github/extensions/superpowers/" "$NewProject/.github/extensions/superpowers/"
```

#### Step 3: Skip or Rewrite .NET-Specific Claude Rules

**Skip entirely** (not applicable to non-.NET):
- `blazor-components.md`
- `ef-core.md`
- `polly-resilience.md`
- `cqrs-mediatr.md`

**Keep and customize:**
- `clean-architecture.md` — Universal, but update layer names for your framework
- `mvp-first.md` — Universal, update tool/framework references
- `memory-optimization.md` — Universal, copy as-is
- `owasp-security.md` — Universal security, remove .NET-specific examples

**Rewrite:**
- `ddd-domain.md` — Your domain model in your language
- `testing-standards.md` — Your test framework (Jest/pytest/go test)

#### Step 4: Rewrite Hooks for Your Build System 🔴

All 10 hooks assume PowerShell + .NET CLI. For a different stack:

| Original Hook | Rewrite For |
|---------------|-------------|
| `build-reminder.ps1` | `npm run build` / `pip install` / `go build` |
| `test-runner.ps1` | `npm test` / `pytest` / `go test` |
| `dotnet-conventions.ps1` | ESLint rules / Black/Ruff / golangci-lint |
| `security-scanner.ps1` | npm audit / safety / govulncheck |

Keep the **hook structure** — just replace the commands inside.

#### Step 5: Rewrite All 5 Instruction Files 🔴

Use the EscrowApp files as structural templates:

1. **AGENTS.md** — Keep the section headings, replace all content with your project's architecture, patterns, conventions, and domain rules
2. **CLAUDE.md** — Keep the reasoning approach sections, replace code examples with your language
3. **GEMINI.md** — Keep the exploration strategy, replace file paths and component references
4. **CODEX.md** — Keep the autonomous execution guardrails structure, replace domain-specific safety rules
5. **copilot-instructions.md** — Keep the structure, rewrite for your framework and conventions

#### Step 5b: Create Antigravity and Codex Config 🟡

For Antigravity:
- Copy `.agent/rules/` — rewrite `.NET-specific rules for your stack, keep universal rules (clean-arch, mvp-first, memory)
- Copy `.agent/workflows/` — rewrite `new-feature.md` and `new-component.md` for your framework
- Copy `.gemini/settings.json` — update project name, type, framework

For Codex CLI:
- Copy `.codex/config.toml` — update project type, framework, protected paths
- Create subdirectory `AGENTS.md` files for your key source directories (~500 bytes each)

#### Step 6: Add Language-Specific Skills

Consider creating new skills for your stack:
- Node.js: `express-developer`, `react-developer`, `prisma-expert`
- Python: `django-developer`, `fastapi-expert`, `sqlalchemy-patterns`
- Go: `go-developer`, `gin-expert`, `gorm-patterns`

Follow the skill template structure from any existing skill in `.github/skills/`.

---

## 5. Architecture Diagram

```
Your Project Root
│
├── .github/
│   ├── skills/                              ← 43 universal skills (COPY AS-IS)
│   │   ├── CATALOG.md                       ← Skill discovery index
│   │   ├── README.md                        ← Usage guide for the skill system
│   │   ├── code-quality/
│   │   │   ├── code-reviewer/
│   │   │   │   ├── SKILL.md                 ← Core methodology (5–10 KB)
│   │   │   │   └── references/              ← Deep-dive files (load on demand)
│   │   │   │       ├── solid-analysis.md
│   │   │   │       └── security-checklist.md
│   │   │   ├── refactor-planner/
│   │   │   ├── code-documenter/
│   │   │   ├── debugging-wizard/
│   │   │   ├── quality-analyzer/
│   │   │   ├── smart-refactor/
│   │   │   └── tech-debt-tracker/
│   │   ├── security/
│   │   │   ├── owasp-audit/
│   │   │   ├── secret-scanner/
│   │   │   ├── threat-modeler/
│   │   │   ├── authentication/              ← .NET-specific
│   │   │   └── authorization/               ← .NET-specific
│   │   ├── architecture/
│   │   ├── testing/
│   │   ├── database/
│   │   ├── devops/
│   │   ├── documentation/
│   │   ├── research/
│   │   ├── project-management/
│   │   ├── ai/
│   │   ├── language/                        ← .NET-specific skills
│   │   └── workflow/
│   │
│   ├── extensions/                          ← Copilot CLI custom tools
│   │   ├── superpowers/                     ← Workflow skills (COPY AS-IS)
│   │   │   └── superpowers.js
│   │   ├── build-guardian/                  ← Build health (REWRITE)
│   │   │   └── build-guardian.js
│   │   ├── context-optimizer/               ← Project summary (REWRITE)
│   │   │   └── context-optimizer.js
│   │   ├── doc-sync/                        ← Docs ↔ code sync (CUSTOMIZE config)
│   │   │   ├── doc-sync.js
│   │   │   └── doc-sync.config.json
│   │   ├── dotnet-conventions/              ← Code style (CUSTOMIZE patterns)
│   │   │   └── dotnet-conventions.js
│   │   ├── research-first/                  ← Docs-before-code (CUSTOMIZE keywords)
│   │   │   └── research-first.js
│   │   └── security-scanner/                ← OWASP scanning (CUSTOMIZE patterns)
│   │       └── security-scanner.js
│   │
│   ├── copilot-instructions.md              ← GitHub Copilot rules (USE TEMPLATE)
│   └── AI-INFRASTRUCTURE-EXPORT-GUIDE.md    ← THIS FILE
│
├── .claude/
│   ├── skills/                              ← Bridge files for /skill discovery (COPY AS-IS)
│   │   ├── code-reviewer/
│   │   │   └── SKILL.md                     ← Redirects to .github/skills/code-quality/code-reviewer/
│   │   ├── owasp-audit/
│   │   │   └── SKILL.md
│   │   └── ... (43 bridge files total)
│   │
│   ├── rules/                               ← Auto-loaded contextual rules
│   │   ├── clean-architecture.md            ← ✅ Copy as-is
│   │   ├── cqrs-mediatr.md                  ← ✅ Copy as-is (.NET)
│   │   ├── blazor-components.md             ← ✅ Copy as-is (.NET/Blazor)
│   │   ├── mvp-first.md                     ← ✅ Copy as-is
│   │   ├── memory-optimization.md           ← ✅ Copy as-is
│   │   ├── ef-core.md                       ← 🟡 Customize DB provider
│   │   ├── owasp-security.md                ← 🟡 Remove domain-specific sections
│   │   ├── polly-resilience.md              ← 🟡 Customize API targets
│   │   ├── testing-standards.md             ← 🟡 Customize framework refs
│   │   └── ddd-domain.md                    ← 🔴 Rewrite for your domain
│   │
│   ├── hooks/                               ← Lifecycle automation scripts
│   │   ├── build-reminder.ps1               ← 🔴 Rewrite for your build tool
│   │   ├── context-optimizer.ps1            ← 🔴 Rewrite for your project
│   │   ├── doc-sync-reminder.ps1            ← 🔴 Rewrite for your doc structure
│   │   ├── dotnet-conventions.ps1           ← 🟡 Customize patterns
│   │   ├── notification.ps1                 ← 🟡 Customize channels
│   │   ├── research-first.ps1               ← 🔴 Rewrite intent keywords
│   │   ├── security-scanner.ps1             ← 🟡 Customize scan patterns
│   │   ├── test-runner.ps1                  ← 🟡 Customize test commands
│   │   └── ... (10 scripts total)
│   │
│   ├── settings.json                        ← Hook wiring + config (CUSTOMIZE)
│   └── settings.local.json                  ← Permissions (COPY AS-IS)
│
├── .agent/
│   ├── rules/                               ← Antigravity-native rules (COPY + CUSTOMIZE)
│   │   ├── GEMINI.md                        ← Entry point with @file references
│   │   ├── clean-architecture.md            ← ✅ Copy as-is
│   │   ├── cqrs-mediatr.md                  ← ✅ Copy as-is (.NET)
│   │   ├── blazor-components.md             ← ✅ Copy as-is (.NET/Blazor)
│   │   ├── mvp-first.md                     ← ✅ Copy as-is
│   │   ├── memory-optimization.md           ← ✅ Copy as-is
│   │   ├── ef-core.md                       ← 🟡 Customize DB provider
│   │   ├── owasp-security.md                ← 🟡 Remove domain-specific sections
│   │   ├── polly-resilience.md              ← 🔴 Rewrite API targets
│   │   ├── ddd-domain.md                    ← 🔴 Rewrite for your domain
│   │   └── testing-standards.md             ← 🟡 Customize framework refs
│   │
│   └── workflows/                           ← Antigravity workflow definitions (COPY + CUSTOMIZE)
│       ├── new-feature.md                   ← 🟡 Customize for your feature pattern
│       ├── security-review.md               ← ✅ Copy as-is
│       ├── build-and-test.md                ← 🟡 Customize build commands
│       └── new-component.md                 ← 🟡 Customize for your UI framework
│
├── .gemini/
│   └── settings.json                        ← Repo-level Gemini config (CUSTOMIZE)
│
├── .codex/
│   ├── config.toml                          ← Codex CLI config (CUSTOMIZE)
│   └── README.md                            ← Directory documentation (CUSTOMIZE)
│
├── .claudeignore                            ← Context exclusions (COPY AS-IS for .NET)
│
├── AGENTS.md                                ← Canonical AI instructions (USE TEMPLATE)
├── CLAUDE.md                                ← Claude-specific extensions (USE TEMPLATE)
├── CODEX.md                                 ← Codex-specific guardrails (USE TEMPLATE)
└── GEMINI.md                                ← Gemini-specific extensions (USE TEMPLATE)
```

### How the Layers Interact

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI Model (Any)                                │
│                                                                  │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐   │
│  │ Copilot CLI│ │ Claude Code│ │  Gemini /  │ │ Codex CLI  │   │
│  │            │ │            │ │ Antigravity│ │            │   │
│  └─────┬──────┘ └─────┬──────┘ └─────┬──────┘ └─────┬──────┘   │
│        │               │              │              │           │
│        ▼               ▼              ▼              ▼           │
│  copilot-        CLAUDE.md      GEMINI.md       CODEX.md        │
│  instructions    + rules        + .agent/rules  + .codex/       │
│  + extensions    + hooks        + .agent/       + subdir         │
│                  + bridges        workflows       AGENTS.md      │
│        │               │              │              │           │
│        └───────┬───────┴──────┬───────┴──────┬───────┘           │
│                ▼              ▼              ▼                    │
│          AGENTS.md      .github/skills/    docs/                 │
│     (canonical truth) (universal methods) (feature docs)         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key insight:** `AGENTS.md` and `.github/skills/` are the **shared foundation**. Tool-specific files (CLAUDE.md, GEMINI.md, CODEX.md, copilot-instructions.md) and their native configurations (`.claude/rules/`, `.agent/rules/`, `.codex/config.toml`, `.github/extensions/`) extend the foundation with tool-specific integration.

---

## 6. Companion Scripts

These scripts automate the export and customization process.

| Script | Purpose | Location |
|--------|---------|----------|
| `export-ai-infrastructure.ps1` | Copies all portable files to a target folder. Generates placeholder templates for project-specific files. Creates a checklist of items that need customization. | `.github/scripts/` |
| `tailor-ai-infrastructure.ps1` | Interactive script that asks about your new project (name, domain, tech stack, entities, patterns) and generates customized AGENTS.md, CLAUDE.md, GEMINI.md, and copilot-instructions.md from templates. | `.github/scripts/` |

### Usage

```powershell
# Export portable infrastructure to a new project
.\.github\scripts\export-ai-infrastructure.ps1 -TargetPath "C:\Projects\MyNewApp"

# Then interactively customize instruction files
.\.github\scripts\tailor-ai-infrastructure.ps1 -TargetPath "C:\Projects\MyNewApp"
```

> **Note:** These scripts may not exist yet. They are planned companion tools. Until they are implemented, follow the manual steps in Section 4.

---

## 7. Maintenance Notes

### Stability by Layer (Most Stable → Most Volatile)

| Layer | Stability | Update Frequency | Trigger |
|-------|-----------|------------------|---------|
| **Skills** (`.github/skills/`) | ⬆️ Very Stable | Rarely | New methodology discovered, skill improvement |
| **Claude Bridge Skills** (`.claude/skills/`) | ⬆️ Very Stable | Only when a universal skill is added/removed | Skill catalog change |
| **Claude Rules** (`.claude/rules/`) | ↗️ Stable | Occasionally | Convention change, new framework adoption |
| **Antigravity Rules** (`.agent/rules/`) | ↗️ Stable | Occasionally | Convention change — keep in sync with Claude rules |
| **Antigravity Workflows** (`.agent/workflows/`) | ↗️ Stable | Rarely | New workflow patterns discovered |
| **Extensions** (`.github/extensions/`) | ➡️ Moderate | When project structure changes | New build tool, directory restructure, new doc category |
| **Hooks** (`.claude/hooks/`) | ➡️ Moderate | When build/CI tools change | New CI pipeline, new test runner, new linter |
| **Codex Config** (`.codex/`) | ↗️ Stable | Rarely | Model change, approval policy change |
| **Subdirectory AGENTS.md** | ➡️ Moderate | When layer patterns change | New patterns, new constraints for a layer |
| **Instruction Files** (AGENTS.md, etc.) | ⬇️ Volatile | Frequently | New feature, architecture change, new entity, convention update |

### Keeping Infrastructure in Sync

1. **When you add a new universal skill:**
   - Create `SKILL.md` + `references/` in `.github/skills/{category}/{skill-name}/`
   - Create bridge file in `.claude/skills/{skill-name}/SKILL.md`
   - Update `.github/skills/CATALOG.md`

2. **When you change project architecture:**
   - Update AGENTS.md (canonical source)
   - Update CLAUDE.md and GEMINI.md (model-specific extensions)
   - Update copilot-instructions.md
   - Update `context-optimizer` extension's project summary

3. **When you add a new build tool or CI step:**
   - Update relevant hooks in `.claude/hooks/`
   - Update `build-guardian` extension if build commands change
   - Update `settings.json` if hook wiring changes

4. **Periodic health checks:**
   - Run `planning_status` tool to check if planning docs are stale
   - Run `docs_status` tool to check if feature docs are stale
   - Review `.github/skills/CATALOG.md` against actual skill directories

5. **When you update Claude rules:**
   - Mirror changes to `.agent/rules/` (same domain knowledge, Antigravity format)
   - Verify rule count stays in sync between `.claude/rules/` and `.agent/rules/`

6. **When you add/change a source layer:**
   - Update the corresponding subdirectory `AGENTS.md` for Codex CLI
   - Verify the Codex instruction stack stays under 32KB

---

## 8. Version History

| Date | Change | Author |
|------|--------|--------|
| 2025-07-14 | Initial export guide created — comprehensive inventory, portability assessment, fastest-path instructions, architecture diagram | AI Infrastructure Team |
| 2026-04-10 | Major update — Added Codex CLI (4th tool), Antigravity native config (.agent/rules, .agent/workflows, .gemini/settings.json), subdirectory AGENTS.md files, .claudeignore, updated all sections for 4-tool parity | AI Infrastructure Team |

---

## Appendix: Quick Reference Card

### Copy-Paste Checklist

```
For a new .NET project, copy these in order:

✅ COPY AS-IS:
  □ .github/skills/           (entire folder — 43 skills)
  □ .claude/skills/           (entire folder — 43 bridge files)
  □ .github/extensions/superpowers/
  □ .claude/settings.local.json
  □ .claudeignore
  □ .github/skills/CATALOG.md
  □ .github/skills/README.md

🟡 COPY + CUSTOMIZE:
  □ .github/extensions/dotnet-conventions/    → adjust patterns
  □ .github/extensions/context-optimizer/     → rewrite summary
  □ .github/extensions/research-first/        → update keywords
  □ .github/extensions/doc-sync/              → remap directories
  □ .claude/rules/ef-core.md                  → update DB provider
  □ .claude/rules/owasp-security.md           → remove domain-specific
  □ .claude/rules/polly-resilience.md         → update API targets
  □ .claude/rules/testing-standards.md        → adjust framework refs
  □ .claude/hooks/ (6 scripts)                → update paths/patterns
  □ .claude/settings.json                     → update hook paths
  □ .agent/rules/ (8 of 11)                   → same as Claude rules edits
  □ .agent/workflows/ (4 files)               → update build commands
  □ .gemini/settings.json                     → update project info
  □ .codex/config.toml                        → update project/safety config
  □ CODEX.md                                  → your project guardrails

🔴 REWRITE:
  □ .github/extensions/build-guardian/        → your build system
  □ .github/extensions/security-scanner/      → your secret patterns
  □ .claude/rules/ddd-domain.md               → your domain model
  □ .agent/rules/ddd-domain.md                → your domain model (Antigravity)
  □ .claude/hooks/ (4 scripts)                → your project context
  □ Subdirectory AGENTS.md (5 files)          → your layer-specific patterns
  □ AGENTS.md                                 → your project identity
  □ CLAUDE.md                                 → your project context
  □ GEMINI.md                                 → your project context
  □ .github/copilot-instructions.md           → your project rules
```

### Search-and-Replace Quick Wins

When customizing instruction files, search for these terms to find project-specific content:

| Search Term | Replace With |
|-------------|--------------|
| `EscrowApp` | Your project name |
| `NexTruzt` | Your product name |
| `EscrowTransaction` | Your primary entity |
| `Stripe` | Your payment/external service |
| `fintech` / `escrow` | Your domain |
| `Held`, `Released`, `Disputed` | Your domain states |
| `IFundHoldable`, `IFundReleasable` | Your strategy interfaces |
| `ClientEmail`, `ConsultantEmail` | Your actor identifiers |
| `PostgreSQL` / `Npgsql` | Your database |
| `Bootstrap 5` | Your CSS framework |
| `es-MX` | Your supported locales |
| `CODEX.md` | Your tool-specific file (if applicable) |
| `.agent/rules/` | Your Antigravity rules path |
| `EscrowApp/Features/` | Your feature/handler directory |
