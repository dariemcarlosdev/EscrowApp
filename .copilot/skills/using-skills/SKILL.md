---
name: using-skills
description: "Master skill for discovering, selecting, and applying all 50 skills in the ecosystem. Start here when beginning any task to find the right skill workflow."
---

# Using Skills — Meta-Skill for the 50-Skill Ecosystem

> Discovery, selection, and orchestration of all specialized skills. The entry point to structured AI-assisted development.

## When to Use

**Use this skill FIRST** when:
- Starting any task or user request
- Unclear which skill applies to the current work
- Multiple skills might be relevant and need chaining
- Learning the skill ecosystem for the first time
- Auditing whether the right skills were applied to previous work

**Always start here.** This is the map to the other 49 skills.

---

## Skill Discovery Flowchart

```
┌─────────────────────────────────────────────────────────────────┐
│                         TASK ARRIVES                             │
└────────────────────────────┬────────────────────────────────────┘
                             │
                ┌────────────▼───────────┐
                │  What kind of work?    │
                └────────────┬───────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
   ┌────▼─────┐         ┌───▼────┐          ┌───▼────┐
   │ PLANNING │         │ CODING │          │ REVIEW │
   └────┬─────┘         └───┬────┘          └───┬────┘
        │                   │                   │
        │                   │                   │
┌───────▼──────────┐    ┌───▼──────────┐   ┌───▼─────────┐
│ Vague idea?      │    │ Implementing │   │ Code review?│
│ → idea-refine    │    │ feature?     │   │ → code-     │
│                  │    │ → increment- │   │   reviewer  │
│ Need spec?       │    │   al-impl    │   │             │
│ → spec-writer    │    │              │   │ Security?   │
│                  │    │ .NET/C#?     │   │ → owasp-    │
│ Task breakdown?  │    │ → csharp-    │   │   audit +   │
│ → feature-forge  │    │   developer  │   │   secret-   │
│                  │    │ → dotnet-    │   │   scanner   │
│ Issue creation?  │    │   core-expert│   │             │
│ → issue-creator  │    │              │   │ Arch review?│
│                  │    │ Verify APIs? │   │ → arch-     │
│ MVP priority?    │    │ → source-    │   │   reviewer  │
│ → mvp-gatekeeper │    │   driven-dev │   │             │
└──────────────────┘    │              │   │ Perf check? │
                        │ Security?    │   │ → query-    │
                        │ → owasp-audit│   │   optimizer │
                        │              │   └─────────────┘
                        │ Database?    │
                        │ → schema-    │
                        │   reviewer   │
                        │ → query-opt  │
                        │              │
                        │ Writing test?│
                        │ → test-gen   │
                        │ → tdd-coach  │
                        │              │
                        │ Refactoring? │
                        │ → smart-ref  │
                        │ → refactor-  │
                        │   planner    │
                        │              │
                        │ Git/version? │
                        │ → git-       │
                        │   workflow   │
                        └──────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ DEBUGGING / OPERATIONS / DOCUMENTATION / AI WORK                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   ┌────▼─────┐     ┌───▼────┐      ┌───▼────┐
   │ BREAKING │     │ DEPLOY │      │ DOCS   │
   └────┬─────┘     └───┬────┘      └───┬────┘
        │               │               │
 ┌──────▼─────┐   ┌────▼────┐    ┌────▼─────┐
 │ Something  │   │ CI/CD?  │    │ README?  │
 │ broke?     │   │ → ci-cd-│    │ → readme-│
 │ → debug-   │   │   builder│   │   gen    │
 │   wizard   │   │         │    │          │
 └────────────┘   │ Deploy? │    │ ADR?     │
                  │ → deploy│    │ → adr-   │
                  │   -pre  │    │   creator│
                  │   flight│    │          │
                  │         │    │ API docs?│
                  │ Monitor?│    │ → api-   │
                  │ → mon-  │    │   doc    │
                  │   expert│    │          │
                  │         │    │ Code doc?│
                  │ Chaos?  │    │ → code-  │
                  │ → chaos-│    │   doc    │
                  │   eng   │    └──────────┘
                  └─────────┘

┌─────────────────────────────────────────────────────────────────┐
│ AI / RESEARCH / ARCHITECTURE                                     │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   ┌────▼─────┐     ┌───▼────┐      ┌───▼────┐
   │ AI WORK  │     │RESEARCH│      │  ARCH  │
   └────┬─────┘     └───┬────┘      └───┬────┘
        │               │               │
 ┌──────▼─────┐   ┌────▼────┐    ┌────▼─────┐
 │ Prompt eng?│   │Exploring│    │ Arch rev?│
 │ → prompt-  │   │codebase?│    │ → arch-  │
 │   engineer │   │ → code- │    │   reviewer│
 │            │   │   base- │    │          │
 │ MCP dev?   │   │   explor│    │ Design   │
 │ → mcp-dev  │   │         │    │ pattern? │
 │            │   │ Research│    │ → design-│
 │ Multi-LLM? │   │spike?   │    │   pattern│
 │ → multi-   │   │ → tech- │    │   -adv   │
 │   agent-   │   │   spike │    │          │
 │   planner  │   │   -plan │    │ Dep chk? │
 │            │   │         │    │ → dep-   │
 │ Agent orch?│   │ Spec    │    │   analyz │
 │ → agent-   │   │ mining? │    │          │
 │   orch     │   │ → spec- │    │ Legacy?  │
 │            │   │   miner │    │ → legacy-│
 │ Token opt? │   │         │    │   modern │
 │ → token-   │   │ Context?│    │          │
 │   opt      │   │ → deep- │    │ Polyglot?│
 │            │   │   ctx-  │    │ → poly-  │
 │ Memory opt?│   │   gen   │    │   glot-  │
 │ → memory-  │   └─────────┘    │   analyz │
 │   opt      │                  └──────────┘
 └────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ QUALITY / TESTING / SECURITY / DATABASE                          │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   ┌────▼─────┐     ┌───▼────┐      ┌───▼────┐
   │ TESTING  │     │SECURITY│      │DATABASE│
   └────┬─────┘     └───┬────┘      └───┬────┘
        │               │               │
 ┌──────▼─────┐   ┌────▼────┐    ┌────▼─────┐
 │ Gen tests? │   │ OWASP?  │    │ Schema?  │
 │ → test-    │   │ → owasp-│    │ → schema-│
 │   generator│   │   audit │    │   reviewer│
 │            │   │         │    │          │
 │ TDD flow?  │   │ Secrets?│    │ Query?   │
 │ → tdd-coach│   │ → secret│    │ → query- │
 │            │   │   -scan │    │   opt    │
 │ Coverage?  │   │         │    └──────────┘
 │ → test-cov │   │ Threat? │
 │   -analyzer│   │ → threat│
 │            │   │   -model│
 │ Quality?   │   │         │
 │ → quality- │   │ Auth?   │
 │   analyzer │   │ → authen│
 │            │   │   -ticate│
 │ Smart ref? │   │ → author│
 │ → smart-   │   │   -ize  │
 │   refactor │   └─────────┘
 │            │
 │ Tech debt? │
 │ → tech-debt│
 │   -tracker │
 │            │
 │ Refactor?  │
 │ → refactor-│
 │   planner  │
 └────────────┘
```

---

## Core Operating Behaviors (Apply to ALL Skills)

These 6 behaviors are **universal** across every skill in the ecosystem:

### 1. Surface Assumptions
**What:** State your assumptions explicitly before implementing.

**Why:** Hidden assumptions cause misalignment. Making them visible lets the user correct course early.

**Example:**
```
❌ "I'll add instant payouts."
✅ "Assuming we want Stripe express payouts with $2 fee passed to consultants. 
   This requires they've completed bank verification. Is that correct?"
```

### 2. Manage Confusion
**What:** When ambiguous, stop and ask clarifying questions. Never guess.

**Why:** Guessing wrong wastes hours. 2 minutes clarifying saves 2 hours rework.

**Example:**
```
❌ "I think you want crypto payouts, implementing Ethereum integration..."
✅ "I see two interpretations:
   1. Instant fiat payouts via Stripe ($2 fee)
   2. Crypto payouts via Ethereum (Web3 wallet integration)
   Which solves your problem?"
```

### 3. Push Back
**What:** Challenge bad approaches honestly. Don't be sycophantic.

**Why:** Agents that say "yes" to everything build the wrong thing. Respectful pushback prevents mistakes.

**Example:**
```
❌ "Sure, I'll build a custom auth system."
✅ "Building custom auth introduces security risk and regulatory complexity.
   Can we use Entra ID or ASP.NET Identity instead? 
   Custom auth should be a last resort."
```

### 4. Enforce Simplicity
**What:** Resist over-engineering. Build the simplest thing that works.

**Why:** Complexity is a liability. MVP means minimum VIABLE, not minimum effort.

**Example:**
```
❌ "I'll create a plugin system for payment providers with hot-swapping."
✅ "We have one provider (Stripe). Let's use the Strategy pattern with DI.
   Plugin architecture when we have 3+ providers, not before."
```

### 5. Maintain Scope Discipline
**What:** Touch only what was asked. Don't fix unrelated issues or add "nice to have" features.

**Why:** Scope creep delays delivery and hides the real cost of changes.

**Example:**
```
❌ "While adding instant payouts, I also refactored the repository pattern 
   and upgraded EF Core."
✅ "Adding instant payouts as requested. I noticed the repository could be 
   refactored — should I create a separate task for that?"
```

### 6. Verify, Don't Assume
**What:** Prove it works. Tests pass, build succeeds, APIs verified against docs.

**Why:** "Seems right" ships bugs. Evidence prevents rework.

**Example:**
```
❌ "This should work, committing."
✅ "Integration test confirms Stripe returns PaymentIntent in <60s. 
   All 47 tests pass. Build succeeds. Committing."
```

---

## Quick Reference: All 50 Skills by Category

| **Category** | **Skill** | **One-Line Summary** |
|---|---|---|
| **CODE QUALITY (7)** | | |
| | `code-reviewer` | Review code for correctness, style, security, maintainability |
| | `refactor-planner` | Analyze code and produce prioritized refactoring plan |
| | `code-documenter` | Generate inline docs, XML comments, usage examples |
| | `debugging-wizard` | Systematic debugging with root cause analysis |
| | `quality-analyzer` | Analyze complexity, maintainability, style conformance |
| | `smart-refactor` | Metrics-driven refactoring with before/after comparison |
| | `tech-debt-tracker` | Detect, quantify, prioritize technical debt |
| **SECURITY (5)** | | |
| | `owasp-audit` | Audit code against OWASP Top 10 vulnerabilities |
| | `secret-scanner` | Detect hardcoded secrets, API keys, credentials |
| | `threat-modeler` | Create STRIDE-based threat models |
| | `authentication` | Implement Entra ID, OIDC, JWT, ASP.NET Identity |
| | `authorization` | Implement policy-based, RBAC, resource-based access |
| **ARCHITECTURE (5)** | | |
| | `architecture-reviewer` | Review system architecture for quality attributes |
| | `design-pattern-advisor` | Recommend and apply appropriate design patterns |
| | `dependency-analyzer` | Analyze dependencies for risks, updates, licenses |
| | `legacy-modernizer` | Plan modernization of legacy codebases |
| | `polyglot-analyzer` | Multi-language quality comparison |
| **TESTING (3)** | | |
| | `test-generator` | Generate unit/integration tests with AAA structure |
| | `tdd-coach` | Guide red-green-refactor TDD cycle |
| | `test-coverage-analyzer` | Analyze coverage gaps, recommend high-value tests |
| **DATABASE (2)** | | |
| | `schema-reviewer` | Review DB schema for normalization, indexing |
| | `query-optimizer` | Analyze and optimize SQL queries |
| **DEVOPS (4)** | | |
| | `ci-cd-builder` | Create or improve CI/CD pipeline configs |
| | `deployment-preflight` | Pre-deployment checks, go/no-go reports |
| | `monitoring-expert` | Design observability with metrics, logs, traces |
| | `chaos-engineer` | Design chaos experiments for resilience |
| **DOCUMENTATION (3)** | | |
| | `readme-generator` | Generate README from project analysis |
| | `adr-creator` | Create Architecture Decision Records |
| | `api-documenter` | Generate API docs with examples and schemas |
| **RESEARCH (4)** | | |
| | `codebase-explorer` | Explore and map unfamiliar codebases |
| | `tech-spike-planner` | Plan time-boxed technical investigations |
| | `spec-miner` | Extract implicit specs from code, tests, docs |
| | `deep-context-generator` | Generate LLM-optimized codebase context |
| | `source-driven-development` | Verify implementations against official documentation |
| **PROJECT MGMT (5)** | | |
| | `spec-writer` | Write comprehensive technical specifications |
| | `issue-creator` | Create structured GitHub issues with subtasks |
| | `feature-forge` | Generate feature breakdowns with stories, tasks |
| | `mvp-gatekeeper` | Enforce MVP scope, block over-engineering |
| | `idea-refine` | Refine vague ideas through divergent/convergent thinking |
| **AI (3)** | | |
| | `mcp-developer` | Build, debug, extend MCP servers and clients |
| | `prompt-engineer` | Write, refactor, evaluate LLM prompts |
| | `agent-orchestrator` | Orchestrate parallel sub-agents with approval gates |
| | `multi-agent-planner` | Plan features via parallel LLMs with adversarial critique |
| **LANGUAGE (2)** | | |
| | `dotnet-core-expert` | Deep .NET 10 expertise — Clean Architecture, EF Core, CQRS |
| | `csharp-developer` | Senior C# 13 developer — records, pattern matching, Blazor |
| **WORKFLOW (2)** | | |
| | `memory-optimization` | Context window optimization, load less, achieve more |
| | `token-optimization` | Create docs/skills that minimize token consumption |
| | `incremental-implementation` | Build features in thin vertical slices |
| | `git-workflow` | Git branching, conventional commits, PR hygiene |
| | `using-skills` | **META-SKILL** — Discovery and invocation of all other skills |

---

## Skill Application Rules

### Rule 1: Check for Applicable Skill Before Starting Work
Before writing any code, documentation, or configuration:
1. Review the flowchart above
2. Identify 1-3 applicable skills
3. Load the skill's `SKILL.md` file
4. Follow its Core Workflow steps in order

### Rule 2: Skills Are Workflows, Not Suggestions
Skills are **checklists with verification gates**, not advisory guidelines.
- ✅ Follow step-by-step in order
- ✅ Complete checkpoints before proceeding
- ❌ Don't cherry-pick steps
- ❌ Don't skip verification sections

### Rule 3: Multiple Skills Can Apply (Chain Them)
Some tasks require skill sequences:

**Example: New feature from vague idea → production**
```
1. idea-refine            → Clarify requirements
2. mvp-gatekeeper         → Validate MVP fit
3. spec-writer            → Write technical spec
4. feature-forge          → Break into tasks
5. source-driven-dev      → Verify Stripe API signatures
6. incremental-impl       → Build slice-by-slice
7. tdd-coach              → Test each slice
8. owasp-audit            → Security scan
9. code-reviewer          → Pre-merge review
10. git-workflow          → Clean commit history
11. deployment-preflight  → Pre-deploy checks
```

### Rule 4: When in Doubt, Start with `spec-writer`
If multiple skills could apply and you're unsure which:
1. Start with `spec-writer` to clarify WHAT you're building
2. Spec will reveal which skills apply next
3. `spec-writer` outputs clear acceptance criteria that guide skill selection

---

## Common Rationalizations (Anti-Skill Patterns)

| Rationalization | Reality |
|---|---|
| "This is too small for a skill" | Small tasks benefit MOST from process — they're where shortcuts create debt. |
| "I can just quickly implement this" | "Quick" implementations skip testing, review, docs. That's not quick, it's unfinished. |
| "I already know how to do this" | Knowing isn't doing. Skills ensure you DON'T skip the verification step. |
| "I'll gather context first, then use a skill" | Use the skill to GUIDE context gathering. That's what `source-driven-development` does. |
| "Skills slow me down" | Rework from skipping skills is 10x slower than following the workflow once correctly. |
| "I only need the skill for complex work" | Skills prevent simple work from BECOMING complex through accumulated shortcuts. |
| "The skill doesn't apply to my situation" | If you're writing code, docs, or config, a skill applies. Find it in the flowchart. |

---

## Lifecycle Sequence for Complete Features

From idea to production:

```
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 1: REQUIREMENTS                                            │
└─────────────────────────────────────────────────────────────────┘
1. idea-refine             → Clarify vague ideas, extract problem
2. mvp-gatekeeper          → Validate MVP scope, block over-engineering
3. spec-writer             → Define detailed requirements with acceptance criteria
4. feature-forge           → Break spec into granular tasks
5. threat-modeler          → (If security-sensitive) Create threat model

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 2: DESIGN & PLANNING                                       │
└─────────────────────────────────────────────────────────────────┘
6. architecture-reviewer   → Review design for quality attributes
7. design-pattern-advisor  → Identify applicable patterns (Strategy, Repository, etc.)
8. schema-reviewer         → (If DB changes) Review schema design
9. tech-spike-planner      → (If unknowns) Plan time-boxed investigation

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 3: IMPLEMENTATION                                          │
└─────────────────────────────────────────────────────────────────┘
10. source-driven-development → Verify API signatures before coding
11. incremental-implementation → Build one vertical slice at a time
12. tdd-coach                  → Test-first for each slice
13. git-workflow               → Atomic commits with conventional messages
14. code-documenter            → (If public API) Generate XML docs

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 4: QUALITY ASSURANCE                                       │
└─────────────────────────────────────────────────────────────────┘
15. test-coverage-analyzer → Identify gaps in test coverage
16. owasp-audit            → Security scan before merge
17. secret-scanner         → Detect hardcoded credentials
18. code-reviewer          → Pre-merge code review
19. quality-analyzer       → Check complexity, maintainability metrics

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 5: DEPLOYMENT                                              │
└─────────────────────────────────────────────────────────────────┘
20. deployment-preflight   → Pre-deploy checks, go/no-go decision
21. ci-cd-builder          → (If pipeline changes) Update CI/CD config
22. monitoring-expert      → (If new service) Add metrics/alerts

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 6: DOCUMENTATION                                           │
└─────────────────────────────────────────────────────────────────┘
23. adr-creator            → Document architectural decisions
24. api-documenter         → (If API changes) Update API docs
25. readme-generator       → (If new project) Generate README
```

---

## Anti-Patterns in Skill Usage

| Pattern | Problem | Fix |
|---|---|---|
| **Skill Skipping** | "I don't need `source-driven-development`, I know the API" → ships hallucinated method | ALWAYS verify APIs against official docs, even if familiar |
| **Partial Skill Application** | Load `tdd-coach`, write test, skip red-green-refactor cycle → untested code | Follow ALL steps in Core Workflow, don't cherry-pick |
| **Skill Paralysis** | Read 10 skills, don't start work → analysis paralysis | Pick ONE skill from flowchart, follow it, chain next skill when done |
| **Wrong Skill Selection** | Use `code-reviewer` when the real need is `refactor-planner` | Start with flowchart, match task type to skill category |
| **Workflow Reversal** | Implement first, THEN check `incremental-implementation` skill | Load skill BEFORE starting work — it guides implementation |
| **Verification Skipping** | Complete workflow, skip verification checklist → untested assumptions | Verification is NOT optional — it's the proof the skill worked |

---

## Red Flags (Skill Ecosystem Violations)

Abort and consult `using-skills` if you observe:

- Starting implementation without identifying applicable skill
- 500+ lines written without following `incremental-implementation` workflow
- API calls written without consulting `source-driven-development`
- Security-sensitive code without `owasp-audit` scan
- Spec written without `mvp-gatekeeper` validation
- Vague requirements accepted without `idea-refine` clarification
- Code merged without `code-reviewer` checklist
- Commits don't follow `git-workflow` conventional format
- Tests written after implementation (violates `tdd-coach`)
- Refactoring started without `refactor-planner` or `smart-refactor`

---

## Skill Loading Instructions

Skills are **markdown files**, not built-in tools. Load them by reading the file:

### Method 1: Direct File Read
```bash
# Read the skill workflow
cat .github/skills/code-quality/incremental-implementation/SKILL.md

# Read a deep-dive reference
cat .github/skills/security/owasp-audit/references/injection-prevention.md
```

### Method 2: View Tool (Windows)
```powershell
# Read skill file
view("C:\path\to\EscrowApp\.github\skills\code-quality\code-reviewer\SKILL.md")
```

### Method 3: Grep for Skill Discovery
```bash
# Find all skills in a category
ls .github/skills/security/
```

**DO NOT:**
- ❌ Try to "invoke" or "call" skills as tools
- ❌ Load all references at once — use progressive disclosure
- ❌ Load skills "just in case" — load when the task matches the trigger

---

## Progressive Disclosure Pattern

Skills have two layers:

### Layer 1: Core Workflow (6-10 KB)
- Load this FIRST
- Contains the step-by-step process
- Includes checkpoints, anti-patterns, verification

### Layer 2: References (Optional Deep-Dives)
- Load ONLY when the Core Workflow directs you to
- Specific topic deep-dives (e.g., "Injection Prevention", "Auth Patterns")
- Referenced in a table: "Load when you need X"

**Example (`owasp-audit`):**
1. Read `SKILL.md` — 8 KB core workflow
2. Core Workflow Step 3 says: "Review SQL code for injection"
3. **NOW** read `references/injection-prevention.md` — 4 KB deep-dive
4. Apply the reference guidance
5. Continue Core Workflow Step 4

**DO NOT load all references at session start.** It wastes context.

---

## Integration with NexTruzt.io Project

### Project-Specific Skill Sequences

**Adding a new payment provider (e.g., PayPal):**
```
1. spec-writer              → Define PayPal integration requirements
2. design-pattern-advisor   → Confirm Strategy pattern applies
3. source-driven-development → Verify PayPal SDK API signatures
4. incremental-implementation → Implement IFundHoldable for PayPal
5. test-generator           → Generate integration tests with PayPal sandbox
6. owasp-audit              → Security review of API key handling
7. code-reviewer            → Pre-merge review
8. adr-creator              → Document "Why PayPal" decision
```

**Refactoring existing code:**
```
1. refactor-planner         → Analyze code, produce refactoring plan
2. smart-refactor           → Execute with baseline metrics
3. test-coverage-analyzer   → Ensure tests cover refactored code
4. code-reviewer            → Verify no behavior changes
5. git-workflow             → Clean commit messages explaining WHY
```

**Debugging production issue:**
```
1. debugging-wizard         → Systematic root cause analysis
2. test-generator           → Add regression test
3. owasp-audit              → (If security-related) Audit the fix
4. git-workflow             → Conventional commit with issue link
5. deployment-preflight     → Validate fix before deploy
```

---

## Verification

Before considering any task complete:

- [ ] Identified applicable skill(s) from flowchart
- [ ] Loaded and read the skill's Core Workflow
- [ ] Followed all workflow steps in order (no cherry-picking)
- [ ] Completed all checkpoints
- [ ] Applied anti-pattern avoidance
- [ ] Verified against the skill's Verification checklist
- [ ] Chained to next skill if task requires multiple skills
- [ ] Did NOT load unnecessary references (progressive disclosure)
- [ ] Did NOT skip steps because "I already know this"

---

## Meta-Rules for This Skill

Since `using-skills` is itself a skill, it follows the same principles:

1. **Surface Assumptions:** "I'm interpreting this as a feature request, so I'm chaining `idea-refine` → `spec-writer`. Correct?"
2. **Manage Confusion:** "This could be a bug fix OR a feature. Which skill applies?"
3. **Push Back:** "You asked me to implement, but requirements are vague. Let me run `idea-refine` first."
4. **Enforce Simplicity:** "You don't need 5 skills for this. `incremental-implementation` + `git-workflow` covers it."
5. **Scope Discipline:** "You asked for code review. I'll run `code-reviewer`, not refactor the entire codebase."
6. **Verify:** "Confirmed: `owasp-audit` passed, `secret-scanner` found no issues, ready to merge."

---

## Skill Ecosystem Philosophy

**Why 50 skills instead of general instructions?**

1. **Reusability:** Workflows are portable across projects and models
2. **Consistency:** Every agent follows the same process
3. **Discoverability:** Flowchart makes it obvious which skill applies
4. **Verifiability:** Checklists prove the workflow was followed
5. **Evolvability:** Update one skill, all agents benefit
6. **Token Efficiency:** Load only what you need, when you need it
7. **Anti-Rationalization:** Explicit tables counter common excuses for shortcuts

**Skills are compounding knowledge.** Each one builds on others, creating a self-reinforcing system for high-quality software delivery.

---

## Final Guidance

- **Start here** for every task
- **Use the flowchart** to find the right skill
- **Load the skill file** and follow its Core Workflow
- **Chain skills** when tasks span multiple concerns
- **Verify** against the skill's checklist before calling it complete
- **Don't skip skills** because they "seem obvious" — that's where bugs hide

Welcome to the 50-skill ecosystem. 🚀
