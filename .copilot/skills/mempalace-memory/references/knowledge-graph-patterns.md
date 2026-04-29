# Knowledge Graph Patterns

> Reference for `mempalace-memory` skill. Load when storing entity relationships, ADRs, or temporal facts in the KG.

---

## What the Knowledge Graph Is

MemPalace's knowledge graph is a **SQLite-backed triple store** with temporal awareness. It stores facts as `(subject, predicate, object)` triples with timestamps, enabling relationship queries and timeline tracking.

**When to use KG vs. drawers:**

| Use Case | KG Triple | Drawer |
|----------|-----------|--------|
| Entity relationships | ✅ `StripePaymentStrategy implements IFundHoldable` | ❌ |
| Architectural decisions with rationale | ❌ | ✅ Full context in `room_decisions` |
| State machine transitions | ✅ `EscrowTransaction.Status transitions Pending→Held→Released` | ❌ |
| Bug fix details | ❌ | ✅ Full symptom/cause/fix in `room_debugging` |
| Dependency relationships | ✅ `HoldFundsHandler depends_on IEscrowTransactionRepository` | ❌ |
| Configuration facts | ✅ `StripeSettings.CaptureMethod equals manual` | ❌ |
| Complex trade-off analysis | ❌ | ✅ Full prose needed |

**Rule of thumb:** If it's a relationship or a fact, use KG. If it needs explanation, use a drawer.

---

## Common Predicate Vocabulary

Use consistent predicates for queryability. These are the recommended predicates for NexSynapse:

### Code Structure Predicates

| Predicate | Meaning | Example |
|-----------|---------|---------|
| `implements` | Class implements interface | `StripePaymentStrategy implements IFundHoldable` |
| `extends` | Class inherits from base | `DomainException extends Exception` |
| `depends_on` | Component depends on another | `HoldFundsHandler depends_on IPaymentStrategyFactory` |
| `contains` | Parent contains child | `Features/Escrow contains HoldFunds, ReleaseFunds, DisputeFunds` |
| `registered_in` | Service registered in DI | `IEscrowTransactionRepository registered_in Program.cs` |
| `lives_in` | Entity belongs to layer/namespace | `EscrowTransaction lives_in Domain/Models` |

### Architecture Predicates

| Predicate | Meaning | Example |
|-----------|---------|---------|
| `pattern` | Design pattern applied | `PaymentProviders pattern Strategy` |
| `transitions_via` | State machine transitions | `EscrowTransaction.Status transitions_via Pending→Held→Released\|Disputed` |
| `publishes` | Component publishes event | `HoldFundsHandler publishes PaymentReceivedEvent` |
| `handles` | Handler processes command | `HoldFundsHandler handles HoldFundsCommand` |
| `validates` | Validator validates command | `HoldFundsCommandValidator validates HoldFundsCommand` |

### Decision Predicates

| Predicate | Meaning | Example |
|-----------|---------|---------|
| `chosen_over` | Architecture decision | `MediatR chosen_over direct_service_calls` |
| `reason` | Why something was chosen | `MediatR reason pipeline_behaviors_and_decoupling` |
| `deprecated_by` | Superseded by newer approach | `EscrowManagerService deprecated_by MediatR_handlers` |
| `blocked_by` | Dependency or blocker | `production_launch blocked_by fintech_attorney_review` |

### Temporal Predicates

| Predicate | Meaning | Example |
|-----------|---------|---------|
| `added_in` | When something was added | `CancelFundsHandler added_in 2025-07-15` |
| `fixed_in` | When a bug was fixed | `NullRef_ExternalRef fixed_in session_2025-07-14` |
| `status` | Current status | `Phase3_Payments status in_progress` |

---

## Example: Mapping the EscrowApp Architecture

```python
# Strategy pattern relationships
mempalace_kg_add(subject="StripePaymentStrategy", predicate="implements", object="IFundHoldable")
mempalace_kg_add(subject="StripePaymentStrategy", predicate="implements", object="IFundReleasable")
mempalace_kg_add(subject="StripePaymentStrategy", predicate="implements", object="IFundCancellable")
mempalace_kg_add(subject="IPaymentStrategyFactory", predicate="resolves", object="IEscrowPaymentStrategy")

# MediatR handler mappings
mempalace_kg_add(subject="HoldFundsHandler", predicate="handles", object="HoldFundsCommand")
mempalace_kg_add(subject="HoldFundsHandler", predicate="depends_on", object="IEscrowTransactionRepository")
mempalace_kg_add(subject="HoldFundsHandler", predicate="depends_on", object="IPaymentStrategyFactory")
mempalace_kg_add(subject="HoldFundsHandler", predicate="publishes", object="PaymentReceivedEvent")

# State machine
mempalace_kg_add(subject="EscrowTransaction.Status", predicate="transitions_via", object="Pending→Held→Released|Disputed")
mempalace_kg_add(subject="Disputed", predicate="blocks", object="Released")

# Layer ownership
mempalace_kg_add(subject="EscrowTransaction", predicate="lives_in", object="Domain/Models")
mempalace_kg_add(subject="HoldFundsHandler", predicate="lives_in", object="Application/Features/Escrow/HoldFunds")
mempalace_kg_add(subject="StripePaymentStrategy", predicate="lives_in", object="Infrastructure/Services/Strategies")
```

## Example: Storing an ADR in the KG

After saving the full ADR as a drawer in `room_decisions`, add queryable triples:

```python
# ADR: Chose MediatR for CQRS
mempalace_kg_add(subject="ADR-001", predicate="decides", object="Use MediatR for CQRS")
mempalace_kg_add(subject="ADR-001", predicate="reason", object="Pipeline behaviors, decoupling, vertical slices")
mempalace_kg_add(subject="MediatR", predicate="chosen_over", object="direct_service_injection")
mempalace_kg_add(subject="ADR-001", predicate="added_in", object="2025-04-01")
```

## Example: Tracking a Bug Fix

After saving details in `room_debugging`, add queryable triples:

```python
# Bug: NullRef on release without prior hold
mempalace_kg_add(subject="BUG-NullRef-Release", predicate="symptom", object="NullReferenceException in ReleaseFundsHandler")
mempalace_kg_add(subject="BUG-NullRef-Release", predicate="root_cause", object="ExternalReference null when no prior HoldFunds call")
mempalace_kg_add(subject="BUG-NullRef-Release", predicate="fixed_in", object="ReleaseFundsHandler.cs guard clause")
mempalace_kg_add(subject="BUG-NullRef-Release", predicate="added_in", object="2025-07-14")
```

---

## Querying Patterns

### Find All Implementations of an Interface

```python
mempalace_kg_query(query="implements IFundHoldable")
# Returns: StripePaymentStrategy
```

### Find All Dependencies of a Handler

```python
mempalace_kg_query(query="HoldFundsHandler depends_on")
# Returns: IEscrowTransactionRepository, IPaymentStrategyFactory, IEventBus
```

### Find All Decisions

```python
mempalace_kg_query(query="chosen_over")
# Returns: All ADR-style decisions with alternatives
```

### Find Bugs Fixed in a Time Period

```python
mempalace_kg_query(query="fixed_in 2025-07")
# Returns: All bugs fixed in July 2025
```

### Trace Event Flow

```python
mempalace_kg_query(query="publishes PaymentReceivedEvent")
# Returns: HoldFundsHandler
mempalace_kg_query(query="subscribes PaymentReceivedEvent")
# Returns: NotificationService, AuditLogger (when implemented)
```

---

## Maintenance Rules

| Rule | Rationale |
|------|-----------|
| Update triples when code changes | Stale KG is worse than no KG |
| Use consistent predicates from the vocabulary | Enables reliable queries |
| Link KG triples to drawer titles | KG for relationships, drawer for context |
| Don't store code in triples | Store the relationship, not the implementation |
| Keep objects concise | `IFundHoldable` not `the IFundHoldable interface in Services/Strategies/` |
| Add temporal predicates (`added_in`, `fixed_in`) | Enables timeline queries |
