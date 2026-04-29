# Palace Organization Patterns

> Reference for `mempalace-memory` skill. Load when setting up wings/rooms or deciding where to store a memory.

---

## The Palace Metaphor

MemPalace uses a spatial metaphor for memory organization:

| Level | Metaphor | Maps To | Example |
|-------|----------|---------|---------|
| **Palace** | The building | Entire MemPalace instance | One per user/workstation |
| **Wing** | A section of the building | A person, project, or domain | `wing_escrowapp` |
| **Room** | A room in a wing | A topic or concern area | `room_payments` |
| **Drawer** | A drawer in a room | A single memory/fact | "Stripe uses manual capture" |

## NexSynapse Palace Layout

### wing_escrowapp — Application Domain Knowledge

This wing stores everything about the EscrowApp itself.

| Room | What Goes Here | Example Drawers |
|------|---------------|-----------------|
| `room_architecture` | Clean Architecture decisions, layer boundaries, pattern choices, DI registrations | "CQRS via MediatR for all business ops", "Strategy pattern for payment providers" |
| `room_payments` | Stripe integration, PaymentIntent flows, idempotency patterns, fee calculations | "Manual capture flow: authorize → hold → capture", "Idempotency key format: {TxId}:{Op}:{Guid}" |
| `room_regulatory` | Compliance rules, terminology restrictions, legal constraints, pre-launch blockers | "Never use 'escrow' in user-facing UI", "Money transmitter license TBD per state" |
| `room_security` | OWASP findings, auth patterns, secret management, API key handling | "A01 broken access control: policy-based auth enforced", "Secrets via Key Vault + Managed Identity" |
| `room_debugging` | Past bugs with symptom → root cause → fix chain | "NullRef on ExternalReference: guard clause added in ReleaseFundsHandler" |
| `room_decisions` | ADR-style records: decision + rationale + rejected alternatives | "Chose Bootstrap 5 over Tailwind: enterprise LOB UI, data-dense dashboards" |

### wing_nexsynapse — AI Infrastructure Knowledge

This wing stores knowledge about the AI development infrastructure itself.

| Room | What Goes Here | Example Drawers |
|------|---------------|-----------------|
| `room_skills` | Skill creation patterns, CATALOG.md conventions, reference file structure | "Skills must be <5KB core, lazy-load references via table" |
| `room_extensions` | Copilot CLI extension patterns, MCP config, tool registration | "Extensions in .github/extensions/, reload via extensions_reload" |
| `room_agents` | Custom agent configurations, agent personas, sub-agent delegation patterns | "test-engineer agent: xUnit + FluentAssertions + TDD patterns" |
| `room_portability` | Cross-model compatibility, bridge files, platform-specific adaptations | "Claude bridges in .claude/skills/, Gemini rules in .agent/rules/" |

### wing_sessions — Auto-Captured Session Context

This wing is primarily populated by diary entries and session hooks.

| Content | How It Gets There | Retrieval Pattern |
|---------|-------------------|-------------------|
| Session summaries | `mempalace_diary_write` at session end | `mempalace_diary_read` at next session start |
| In-progress work state | `mempalace_diary_write` before `/compact` | Search for "in progress" or "next steps" |
| Cross-session continuity | Auto-save hooks (every 15 messages) | `mempalace_wake_up` loads recent context |

---

## When to Create a New Wing

Create a new wing when:

| Signal | Action |
|--------|--------|
| Starting work on a completely new project | `wing_{project_name}` |
| A new person's preferences need tracking | `wing_{person_name}` |
| A domain area grows beyond 50+ drawers across rooms | Consider splitting into a dedicated wing |

**Do NOT create wings for:** temporary tasks, one-off investigations, or session-specific state. Use `wing_sessions` diary for those.

## When to Create a New Room

Create a new room when:

| Signal | Action |
|--------|--------|
| A topic has 10+ drawers and is distinct from existing rooms | New room in the relevant wing |
| Cross-cutting concern doesn't fit existing rooms | New room (e.g., `room_performance`, `room_localization`) |
| A new feature area emerges with its own vocabulary | New room (e.g., `room_web3_bridge`) |

**Do NOT create rooms for:** individual features (use drawers in the relevant topic room), individual sessions (use diary), or temporary experiments.

---

## Naming Conventions

| Element | Convention | Examples |
|---------|-----------|----------|
| Wing | `wing_{project_or_person}` lowercase, underscores | `wing_escrowapp`, `wing_nexsynapse` |
| Room | `room_{topic}` lowercase, underscores | `room_payments`, `room_debugging` |
| Drawer title | Brief, descriptive, searchable | "Stripe manual capture flow", "NullRef on release without hold" |

## Drawer Content Best Practices

### Good Drawer Content

```
Title: Stripe PaymentIntent manual capture flow
Content: Hold: Create PI with capture_method=manual → authorize only.
Release: Call pi.capture() to move funds. Cancel: Call pi.cancel() to void.
Idempotency key required on all three ops. ExternalReference stores PI ID.
```

### Bad Drawer Content

```
Title: Stripe stuff
Content: We use Stripe for payments. It has an API. You call it with a key.
```

**Rules:**
- Titles should be search-friendly — someone querying "manual capture" or "PaymentIntent" should find it
- Content should answer a question — "How does X work?" or "Why did we choose Y?"
- Include the **why**, not just the **what** — rationale is more valuable than facts
- Reference file paths or handler names when relevant — `HoldFundsHandler`, `Services/Strategies/`

---

## Retrieval Strategy

### Semantic Search (mempalace_search)

Use for: fuzzy concept matching, finding related memories, exploring a topic area.

```
mempalace_search(query="payment authorization flow", wing="wing_escrowapp", top_k=5)
```

ChromaDB embeddings find semantically similar content even with different wording.

### Room Browsing (mempalace_browse_room)

Use for: reviewing all memories in a specific topic area, auditing stored knowledge.

```
mempalace_browse_room(wing="wing_escrowapp", room="room_decisions")
```

### Knowledge Graph (mempalace_kg_query)

Use for: relationship queries, "what implements X?", "what depends on Y?", temporal facts.

```
mempalace_kg_query(query="implements IFundHoldable")
```

### Decision Tree for Retrieval

```
Need to find something specific?
  → Know the room?  → mempalace_browse_room
  → Don't know where? → mempalace_search with wing filter
  → Need relationships? → mempalace_kg_query
  → Need session history? → mempalace_diary_read
  → First time in session? → mempalace_wake_up (loads recent + relevant)
```
