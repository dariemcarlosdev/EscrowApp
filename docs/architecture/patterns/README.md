# Design Patterns — EscrowApp

> Reusable architectural and behavioral patterns used across the NexTruzt.io EscrowApp codebase.
> 
> Each pattern document includes: **Intent**, **Participants**, **Structure**, **Consequences**, **Trade-offs**, and **Code Examples**.

---

## Quick Index

| Pattern | Category | Complexity | Status | Reference |
|---------|----------|-----------|--------|-----------|
| [Observational Webhook Handler](#observational-webhook-handler) | **Behavioral** | Medium | ✅ MVP | `observational-webhook-handler.md` |
| Strategy Pattern | **Structural** | Low | ✅ Implemented | See below |
| Repository Pattern | **Structural** | Low | ✅ Implemented | See below |
| Vertical Slice Architecture | **Architectural** | Low | ✅ Implemented | See below |
| MediatR CQRS | **Architectural** | Medium | ✅ Implemented | See below |
| Code-Behind (Blazor) | **Presentation** | Low | ✅ Implemented | See below |

---

## Post-MVP Patterns Roadmap

Advanced patterns for production reliability and audit compliance:

| Phase | Patterns | Timeline | Documentation |
|-------|----------|----------|-----------------|
| **v1.1** | Event Deduplication, Event Sourcing, Outbox | 4-6 weeks post-MVP | `docs/planning/post-mvp/v1.1-roadmap.md` |
| **v1.2** | Saga Pattern, Dead Letter Queue | 8-10 weeks post-MVP | — |
| **v1.3+** | Event Enrichment, Circuit Breaker | Post-v1.2 | — |

📋 **Task Tracking:** See `docs/planning/post-mvp/v1.1-roadmap.md` for detailed pattern descriptions, implementation sequence, and SQL task breakdown (tc-12 through tc-17).

**Key Patterns:**
- **tc-12 Event Deduplication** — Prevent duplicate webhook events from Stripe retries
- **tc-13 Event Sourcing** — Complete audit trail (append-only event log)
- **tc-14 Outbox Pattern** — Guarantee domain event delivery post-commit

---

## Observational Webhook Handler

**File:** [`observational-webhook-handler.md`](./observational-webhook-handler.md)

**Category:** Behavioral Pattern

**Problem Solved:** 
- Async webhooks from external providers (Stripe, PayPal) must not drive critical business state
- Risk: webhook retries, replay attacks, or timing issues cause duplicate state changes
- Current practice: most webhook handlers transition state, risking corruption

**Solution:**
- Webhook receives notification but confirms existing state instead of driving it
- State transitions happen synchronously (HoldFundsCommand)
- Webhook publishes domain events for downstream listeners (emails, dashboards) without mutating core data

**Example:** 
Stripe's `payment_intent.succeeded` webhook receives notification that a hold succeeded. Instead of transitioning transaction status from "Pending" → "Held", the webhook confirms the hold already happened in HoldFundsCommand. Status stays "Held". Webhook publishes PaymentReceivedEvent for audit trail and downstream workflows.

**Trade-off:**
- ✅ **Pro:** Safe, idempotent, fail-safe (if webhook is down, business logic still works)
- ❌ **Con:** Requires dual state management (sync for critical changes, async for confirmation)

**Implementation Reference:** See [`minimal-webhook-handler-mvp.md`](../stripe-webhooks/minimal-webhook-handler-mvp.md) for Stripe webhook example.

---

## Strategy Pattern

**Where Used:**
- `Services/Strategies/IEscrowPaymentStrategy` — Payment provider abstraction
- Implementations: `StripePaymentStrategy`, future `PayPalPaymentStrategy`, `EthereumPaymentStrategy`

**Example:** Same Hold/Release commands work for Stripe, PayPal, or Ethereum via strategy interface.

---

## Repository Pattern

**Where Used:**
- `Models/Repositories/IEscrowTransactionRepository` — Data access abstraction
- Implementation: `Data/Repositories/EscrowTransactionRepository`

**Benefit:** EF Core internals hidden from business logic; easy to swap for raw SQL, Dapper, or other ORM.

---

## Vertical Slice Architecture

**Where Used:**
- `Features/Escrow/HoldFunds/` — Complete hold feature
- `Features/Escrow/ReleaseFunds/` — Complete release feature
- Each slice: Command → Handler → Repository → Domain Events

**Benefit:** Features are self-contained, easy to develop and test in isolation.

---

## MediatR CQRS

**Where Used:**
- All feature commands and queries dispatch via `IMediator`
- Commands modify state (HoldFundsCommand, ReleaseFundsCommand)
- Queries read state (GetTransactionQuery, ListTransactionsQuery)

**Benefit:** Clear separation of read/write concerns, testability, extensibility.

---

## Code-Behind Pattern

**Where Used:**
- All Blazor components: `.razor` (markup) + `.razor.cs` (logic) + `.razor.css` (styles)

**Example:**
```
Login.razor          ← markup + @inject
Login.razor.cs       ← sealed partial class, all logic
Login.razor.css      ← scoped styles
```

**Benefit:** Cleaner separation of concerns, IDEs with better IntelliSense, easier to test.

---

## Document Updates

When adding a new pattern to the codebase:

1. Create a new `.md` file in this folder (e.g., `new-pattern.md`)
2. Add an entry to the **Quick Index** table above
3. Include: Intent, Problem, Solution, Code Example, Trade-offs
4. Update `docs/README.md` and `docs/architecture/README.md` to reference the patterns folder
5. Link from relevant feature/architecture docs

---

## See Also

- [`docs/platform/architecture/overview/architecture-overview.md`](../overview/architecture-overview.md) — High-level architecture
- [`docs/platform/architecture/event-bus/event-bus.md`](../event-bus/event-bus.md) — Domain event publishing
- [`docs/modules/system/testing/testing-strategy.md`](../../modules/system/testing/testing-strategy.md) — Testing patterns
