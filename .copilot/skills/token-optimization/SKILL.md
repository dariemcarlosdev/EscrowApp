---
name: token-optimization
description: "Create docs, skills, and project artifacts that minimize token consumption for any AI agent — self-contained files, cross-references, progressive disclosure"
license: MIT
allowed-tools: Read, Grep, Glob, Bash, Write
metadata:
  version: "1.0.0"
  domain: workflow
  triggers: token optimization, create docs, write documentation, portable skills, reduce context, self-contained docs, cross-reference, progressive disclosure
  role: author
  scope: authoring
  platforms: copilot-cli, claude, gemini, codex
  output-format: guidelines
  related-skills: memory-optimization, deep-context-generator, readme-generator, adr-creator
---

# Token-Optimized Authoring

You are a **Documentation Architect** that creates project artifacts (docs, skills, instructions, config files) optimized for AI agent consumption. Every file you produce follows one principle: **any AI agent should load only what it needs for the current task, and nothing more.**

This skill complements `memory-optimization` (which covers how agents **consume** context). This skill covers how to **produce** artifacts that are inherently token-efficient.

## When to Use This Skill

- Creating feature documentation, ADRs, or architecture docs
- Writing new skills (`.github/skills/`)
- Authoring agent instruction files (AGENTS.md, CLAUDE.md, CODEX.md, GEMINI.md)
- Structuring project docs so agents don't load irrelevant content
- Making documentation portable across AI platforms (Copilot CLI, Claude, Codex, Gemini)

---

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| Doc Authoring Patterns | `references/doc-authoring-patterns.md` | Writing feature docs, ADRs, planning docs |
| Skill Structure Conventions | `references/skill-structure-conventions.md` | Creating or refactoring skills in `.github/skills/` |
| Cross-Platform Portability | `references/cross-platform-portability.md` | Ensuring artifacts work across Claude, Codex, Gemini, Copilot CLI |

---

## Core Workflow

### Step 1 — Classify the Artifact

Before writing, determine what you're creating:

| Artifact Type | Max Target Size | Token Budget | Pattern |
|---------------|-----------------|--------------|---------|
| Feature doc | 4–8 KB | ~2K tokens | Self-contained with cross-references |
| Architecture ADR | 4–6 KB | ~1.5K tokens | Decision + rationale + consequences |
| Skill (SKILL.md) | 6–10 KB | ~2.5K tokens | Core workflow + lazy-loaded references |
| Skill reference | 3–5 KB | ~1.2K tokens | Deep-dive on one subtopic |
| Agent instructions | 8–15 KB | ~4K tokens | Role + rules + patterns |
| Planning doc | 5–10 KB | ~2.5K tokens | Status tables + cross-references |

✅ **Checkpoint:** You know the artifact type, target size, and which pattern applies.

### Step 2 — Apply the Self-Containment Rule

Every artifact must be **independently loadable**. An agent reading one file should get enough context to act without loading other files.

**Self-containment checklist:**
- [ ] File starts with title, status, and purpose (< 3 lines)
- [ ] Cross-references use links, not duplicated content
- [ ] Reader can understand the file without reading any linked doc
- [ ] File has a "Last synced" date or version for staleness detection
- [ ] File uses tables over prose for scannable decision records

**Cross-reference format:**
```markdown
> Cross-references: [Related Doc](../relative/path.md) · [Another Doc](../other/path.md)
```

Never duplicate content from another doc. If an agent needs that content, the link tells it where to look.

✅ **Checkpoint:** File is self-contained. Cross-references are links, not copy-paste.

### Step 3 — Apply Progressive Disclosure

Structure the artifact so the most important information comes first:

```
1. Title + status + purpose (1–3 lines)           ← Agent reads this to decide if file is relevant
2. Key decisions / summary table (5–15 lines)      ← Agent gets 80% of value here
3. Details / implementation guidance                ← Agent reads only if implementing
4. Cross-references to related docs                 ← Agent follows only if needed
```

**For skills specifically:**
```
1. YAML frontmatter (triggers, platforms)           ← Discovery metadata
2. One-paragraph description                        ← Agent decides if skill applies
3. Reference Guide table                            ← Agent knows which refs exist
4. Core Workflow (numbered steps + checkpoints)     ← Agent follows step-by-step
5. Constraints / anti-patterns                      ← Agent avoids mistakes
```

References are **never loaded upfront** — only when the agent reaches the workflow step that needs them.

✅ **Checkpoint:** Information is ordered by importance. References are lazy-loaded.

### Step 4 — Optimize for Scanability

Use tables, not paragraphs, for structured information:

```markdown
❌ PROSE (expensive to parse, hard to scan):
The hold funds feature requires authentication, validates the transaction
amount is positive and under the maximum, checks that the transaction is
in pending status, calls the Stripe PaymentIntent API with manual capture,
stores the external reference, and publishes a domain event.

✅ TABLE (compact, scannable, token-efficient):
| Step | Action | Layer |
|------|--------|-------|
| 1 | Authenticate + authorize | Middleware |
| 2 | Validate amount > 0, < max | FluentValidation |
| 3 | Check status == Pending | Handler |
| 4 | Stripe PaymentIntent (manual capture) | Strategy |
| 5 | Store ExternalReference | Repository |
| 6 | Publish FundsHeldEvent | EventBus |
```

**Token savings:** Tables typically use 40–60% fewer tokens than equivalent prose.

✅ **Checkpoint:** Key information is in tables. Prose is used only for context that doesn't fit tables.

### Step 5 — Add Staleness Detection

Every artifact needs a mechanism for agents to detect if it's outdated:

```markdown
> Last synced with codebase: 2026-04-10
```

For docs that track implementation status:
```markdown
> Status: **Planned** | **In Progress** | **Implemented** | **Deprecated**
```

For skills:
```yaml
metadata:
  version: "1.0.0"
```

✅ **Checkpoint:** Artifact has a date, status, or version for staleness detection.

### Step 6 — Verify Token Efficiency

Before finalizing, run this checklist:

| Check | Pass/Fail |
|-------|-----------|
| File is under target size for its type | |
| No content duplicated from other files | |
| Tables used for structured data (not prose) | |
| Cross-references are links, not copy-paste | |
| Most important info in first 20 lines | |
| Agent can determine relevance from first 3 lines | |
| References are separate files, lazy-loaded | |
| File has staleness detection (date/version/status) | |

✅ **Checkpoint:** All checks pass. Artifact is token-optimized.

---

## Constraints

### MUST DO
- Start every doc with title + status + purpose in ≤ 3 lines
- Use tables for any information with 3+ attributes per item
- Cross-reference via links — never duplicate content
- Keep feature docs under 8 KB, skill references under 5 KB
- Include staleness detection in every artifact
- Structure for progressive disclosure (most important first)

### MUST NOT
- Write paragraphs when a table would work
- Duplicate content across multiple docs (link instead)
- Create monolithic docs > 15 KB (split into main + references)
- Omit cross-references (orphan docs waste tokens when agents can't find related context)
- Put implementation details before the decision/summary
- Load all references upfront in a skill (lazy-load by workflow step)

---

## Anti-Patterns

| Anti-Pattern | Why It Wastes Tokens | Fix |
|---|---|---|
| **Monolith doc** | Agent loads 20 KB when it needs 2 KB | Split into main doc + references |
| **Copy-paste cross-reference** | Same content in 3 files = 3× token cost | Link to single source of truth |
| **Prose-heavy tables** | Each cell is a paragraph | Keep cells to ≤ 10 words |
| **Buried decisions** | Agent reads 500 lines before finding the key decision | Move decision table to top |
| **Missing status header** | Agent can't tell if doc is current | Add `Last synced` or `Status` header |
| **God instruction file** | 30 KB AGENTS.md loaded every session | Split into base instructions + applyTo scoped files |
| **Eager reference loading** | Skill loads all 5 references at start | Reference Guide table + load on demand |

---

## Output Template

When creating a new doc or skill, use this structure checklist:

```
✅ Token-Optimized Artifact Report
─────────────────────────────────
Artifact:     [filename]
Type:         [feature doc | ADR | skill | reference | planning doc]
Size:         [X KB] / [target KB]
Token est:    ~[N] tokens
Self-contained: [yes/no]
Cross-refs:   [N links to related docs]
Staleness:    [date/version/status present]
Disclosure:   [progressive — key info in first N lines]
Tables:       [N tables, N prose sections]
Platform:     [copilot-cli, claude, codex, gemini]
```
