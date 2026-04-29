---
name: mempalace-memory
description: "Cross-session persistent memory via MemPalace — semantic search, temporal knowledge graph, and auto-save hooks"
license: MIT
allowed-tools: mempalace_search, mempalace_add_drawer, mempalace_browse_palace, mempalace_browse_room, mempalace_kg_add, mempalace_kg_query, mempalace_diary_read, mempalace_diary_write, mempalace_wake_up, Read, Grep
metadata:
  version: "1.0.0"
  domain: workflow
  triggers: remember, memory, recall, cross-session, knowledge graph, palace, search history, persistent memory, save context
  role: expert
  scope: memory-management
  platforms: copilot-cli, claude, gemini, codex
  output-format: guidelines
  related-skills: memory-optimization, token-optimization, using-skills, debugging-wizard
---

# MemPalace Memory

Persistent cross-session memory using MemPalace's MCP tools. Store and retrieve decisions, debugging insights, architectural knowledge, and regulatory context across sessions via semantic vector search (ChromaDB) and a temporal knowledge graph (SQLite).

**Key idea:** Every AI session is ephemeral. MemPalace makes knowledge permanent. What you learn in session N is instantly available in session N+100.

## When to Use This Skill

| Moment | Action | Why |
|--------|--------|-----|
| Session start | `mempalace_wake_up` + search for context | Load relevant memories before planning |
| Before planning a feature | `mempalace_search` for prior decisions | Avoid re-debating settled architecture |
| After fixing a bug | `mempalace_add_drawer` + `mempalace_kg_add` | Never debug the same issue twice |
| After an architecture decision | `mempalace_add_drawer` to `room_decisions` | Preserve rationale for future sessions |
| Before `/compact` | `mempalace_diary_write` with session summary | Survive context reset |
| When debugging | `mempalace_search` for similar past bugs | Find solutions from prior sessions |
| After learning something new | `mempalace_add_drawer` | Build cumulative project knowledge |

---

## Reference Guide

| Topic | Reference File | Load When |
|-------|---------------|-----------|
| Palace Organization | `references/palace-organization.md` | Setting up wings/rooms, choosing where to store memories |
| Knowledge Graph Patterns | `references/knowledge-graph-patterns.md` | Storing entity relationships, ADRs, temporal facts |
| Hooks & Automation | `references/hooks-and-automation.md` | Configuring auto-save, wake-up, integration with NexSynapse workflows |

---

## Core Workflow

### Step 1 — Wake Up and Load Context

At session start, wake up MemPalace and search for relevant context:

```
mempalace_wake_up()
mempalace_search(query="[current task or project area]", top_k=5)
```

Scan results for: prior decisions, known bugs, architectural constraints, regulatory notes.

✅ **Checkpoint:** You have checked palace memories before starting work. No blind starts.

### Step 2 — Search Before Deciding

Before making any architectural or implementation decision, search for prior art:

```
mempalace_search(query="[decision topic]", wing="wing_escrowapp", top_k=5)
mempalace_kg_query(query="[entity or concept]")
```

If a prior decision exists, honor it unless explicitly overridden by the user. Reference the drawer ID in your reasoning.

✅ **Checkpoint:** Prior decisions checked. Not re-debating settled matters.

### Step 3 — Save After Learning

After completing meaningful work, persist the knowledge:

| What Happened | Where to Save | Tool | Format |
|---------------|---------------|------|--------|
| Architecture decision | `room_decisions` | `mempalace_add_drawer` | Decision + rationale + alternatives rejected |
| Bug fix | `room_debugging` | `mempalace_add_drawer` + `mempalace_kg_add` | Symptom → Root cause → Fix |
| Security finding | `room_security` | `mempalace_add_drawer` | OWASP category + finding + remediation |
| Regulatory insight | `room_regulatory` | `mempalace_add_drawer` | Rule + context + source |
| Pattern learned | `room_architecture` | `mempalace_add_drawer` | Pattern + when to apply + example |

Use AAAK compression for routine saves. Use plain text for nuanced decisions where compression would lose meaning.

```
mempalace_add_drawer(
  wing="wing_escrowapp",
  room="room_decisions",
  title="Chose MediatR over direct service calls",
  content="Decision: Use MediatR CQRS for all business ops. Rationale: Decouples UI from business logic, enables pipeline behaviors (validation, logging). Rejected: Direct service injection — tight coupling, no cross-cutting pipeline."
)
```

✅ **Checkpoint:** New knowledge persisted. Future sessions will find it.

### Step 4 — Use the Knowledge Graph for Relationships

For facts that have relationships or temporal relevance, use the knowledge graph:

```
mempalace_kg_add(
  subject="StripePaymentStrategy",
  predicate="implements",
  object="IFundHoldable, IFundReleasable, IFundCancellable"
)

mempalace_kg_add(
  subject="EscrowTransaction.Status",
  predicate="transitions_via",
  object="Pending → Held → Released | Disputed"
)
```

Query the graph when you need to understand relationships:

```
mempalace_kg_query(query="What implements IFundHoldable?")
mempalace_kg_query(query="EscrowTransaction state transitions")
```

✅ **Checkpoint:** Entity relationships stored in KG, not just free-text drawers.

### Step 5 — Write Session Diary Before Context Loss

Before `/compact`, long session breaks, or session end, write a diary entry:

```
mempalace_diary_write(
  content="Session: Implemented CancelFunds handler. Added IFundCancellable to StripePaymentStrategy. Created FluentValidation validator. Updated planning docs. Next: Integration tests for cancel flow."
)
```

✅ **Checkpoint:** Session state captured. Next session can resume seamlessly.

---

## AAAK Compression Guide

MemPalace uses AAAK 30× compression for token-efficient storage. Use it for routine factual memories:

| Plain Text | AAAK Compressed |
|------------|-----------------|
| The EscrowTransaction entity uses manual capture via Stripe PaymentIntents | `EscrowTx→Stripe PI manual_capture` |
| We decided to use policy-based auth instead of role checks | `Auth: policy-based > role strings. Central AuthorizationPolicies class` |
| Bug: null ref when ExternalReference not set before release | `BUG: NullRef on ExternalRef when releasing w/o prior hold. FIX: guard clause in ReleaseFundsHandler` |

**When NOT to compress:** Complex architectural rationale, regulatory compliance notes, nuanced trade-off discussions. These need full text for accurate recall.

---

## Palace Organization (Quick Reference)

```
wing_escrowapp/           ← Application domain knowledge
  room_architecture/      ← Clean Arch, CQRS, Strategy, Repository patterns
  room_payments/          ← Stripe, idempotency, manual capture, PaymentIntents
  room_regulatory/        ← Compliance rules, terminology, legal constraints
  room_security/          ← OWASP findings, auth patterns, secret management
  room_debugging/         ← Past bugs: symptom → cause → fix
  room_decisions/         ← ADRs: decision + rationale + rejected alternatives

wing_nexsynapse/          ← AI infrastructure knowledge
  room_skills/            ← Skill creation patterns, catalog conventions
  room_extensions/        ← Extension development, MCP config
  room_agents/            ← Agent config, custom agent personas
  room_portability/       ← Cross-model compatibility rules

wing_sessions/            ← Auto-captured per session (diary entries)
```

→ For detailed organization patterns, load `references/palace-organization.md`.

---

## Constraints

### MUST DO
- Call `mempalace_wake_up` at session start
- Search palace before making decisions that may have prior art
- Save decisions, bug fixes, and architectural insights after completing them
- Use the knowledge graph for entity relationships and state machines
- Write a diary entry before `/compact` or session end
- Use AAAK compression for routine facts; plain text for nuanced decisions

### MUST NOT
- Start a session without checking palace memories for the current task
- Re-decide settled architectural questions without checking `room_decisions`
- Store sensitive data (API keys, passwords, PII) in palace memories
- Skip saving after significant debugging sessions — the fix is worth remembering
- Over-compress complex rationale — clarity beats token savings for decisions
- Store raw code blocks — store the insight about the code, not the code itself

---

## Anti-Rationalization Table

| Excuse | Why It's Wrong | Do This Instead |
|--------|---------------|-----------------|
| "I'll remember this next session" | You won't. Context resets. | `mempalace_add_drawer` now |
| "This bug fix is too trivial to save" | Trivial bugs recur. 5-min fix now = 30-min debug later | Save symptom + cause + fix |
| "I don't need prior context for this" | You might be re-debating a settled decision | `mempalace_search` first, takes 2 seconds |
| "AAAK compression is too complex" | Use plain text. AAAK is optional optimization | Save in any format > not saving at all |
| "The knowledge graph is overkill" | KG enables relationship queries plain text can't | Use KG for has/implements/depends relationships |

---

## Integration with NexSynapse Workflows

| Workflow | MemPalace Integration |
|----------|----------------------|
| `systematic-debugging` | Search `room_debugging` before investigating. Save root cause after fix. |
| `executing-plans` | Search `room_decisions` for constraints. Save completed task insights. |
| `writing-plans` | Search for prior architecture decisions. Save the plan rationale. |
| `verification-before-completion` | Check KG for expected relationships. Verify against saved patterns. |
| `tdd` | Search for past test patterns. Save new testing insights. |

---

## Output Template

When reporting memory operations:

```
## 🏛️ Palace Memory Status
- **Wake-up:** [loaded / skipped]
- **Memories retrieved:** [N results from search]
- **Memories saved:** [N drawers added]
- **KG triples added:** [N relationships]
- **Diary written:** [yes / no]
- **Key recall:** [brief summary of most relevant retrieved memory]
```
