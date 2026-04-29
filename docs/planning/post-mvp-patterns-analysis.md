# Post-MVP Webhook Patterns Analysis

> Strategic deep-dive into 7 advanced webhook patterns for fintech platform evolution.  
> **Audience:** Architects, senior developers, technical stakeholders  
> **Target Timeline:** v1.1 (4-6 weeks post-MVP) through v1.3+ (post-v1.2)  
> **Status:** 📋 Planning

---

## Executive Summary

The Observational Webhook Handler is a **safe, simple MVP pattern** that assumes webhooks only confirm already-committed operations. Post-MVP patterns address real-world complexity:

| Phase | Pattern | Complexity | Priority | Key Benefit | Tracking |
|-------|---------|-----------|----------|-------------|----------|
| **MVP** | Observational Handler | Low | 🟢 SHIP | Prevents state corruption | ✅ Complete |
| **v1.1** | Event Deduplication | Medium | 🟢 HIGH | Eliminate duplicate events | tc-12 |
| **v1.1** | Event Sourcing | High | 🟢 HIGH | Complete audit trail | tc-13 |
| **v1.1** | Outbox Pattern | Medium | 🟢 HIGH | Guarantee delivery | tc-14 |
| **v1.2** | Saga Pattern | High | 🟡 MEDIUM | Long-running workflows | tc-15 |
| **v1.2** | Dead Letter Queue | Medium | 🟡 MEDIUM | Handle poison events | tc-16 |
| **v1.3** | Event Enrichment | Low | 🟠 LOW | Add context to events | tc-17 |
| **v1.3+** | Circuit Breaker | Low | 🟠 LOW | Resilience for failures | tc-18 |

---

## 1. Event Deduplication (v1.1) — HIGH PRIORITY

**Problem:**
- Stripe retries failed webhooks (up to 5 attempts with exponential backoff)
- Same webhook event can be delivered multiple times
- Currently MVP accepts duplicate `PaymentReceivedEvent` publications
- Risk: Downstream listeners (email, dashboard, accounting) process duplicate transactions

**Solution Options:**

### A) Event ID Cache (Simple, Recommended for MVP++)

```
Incoming webhook:
  1. Extract event_id from Stripe signature header
  2. Check in-memory cache (Redis, Memcached)
  3. If exists → return 204 (idempotent response)
  4. If new → process + add to cache (TTL 24h)
  5. Publish PaymentReceivedEvent (once per unique event_id)
```

**Pros:**
- ✅ Minimal code changes (add cache check)
- ✅ Fast (cache lookup < 1ms)
- ✅ Scales to multiple instances (with Redis)

**Cons:**
- ❌ Cache infrastructure (Redis) required
- ❌ Loss of dedup data on cache restart

### B) Database Idempotency Key (Recommended for Production Fintech)

```
Create Webhooks table:
  (event_id PK, event_type, payload JSON, created_at, 
   processed_at, status, error_message)

Incoming webhook:
  1. Extract event_id
  2. Try INSERT OR IGNORE INTO Webhooks (event_id, ...)
  3. If new row: process
  4. If existing row: skip (already processed)
  5. Publish event
```

**Pros:**
- ✅ Audit trail (every webhook attempt recorded)
- ✅ Natural deduplication at DB layer
- ✅ Survives process crash/restart
- ✅ Regulatory value (proof of processing)

**Cons:**
- ❌ Extra database table
- ❌ Query per webhook (minimal overhead)

**Recommendation:** **Use Option B** for production fintech — audit trail is legally valuable.

---

## 2. Event Sourcing (v1.1) — HIGH PRIORITY

**Problem:**
- Current model: `EscrowTransaction.Status` is single point (Pending → Held → Released)
- Cannot answer: "What happened to this transaction and when?"
- Debugging nightmare: "Why is this transaction stuck in Held status?"
- Regulatory audit: "Reconstruct complete timeline for this transaction"
- No history of failed attempts or corrections

**Solution:**

Instead of mutating `Status` field:

```csharp
// Current (pre-Event Sourcing):
UPDATE EscrowTransaction SET Status = 'Held' WHERE Id = 42

// Post-Event Sourcing:
INSERT INTO PaymentEvents 
  (TransactionId, EventType, Timestamp, Payload, Actor)
VALUES 
  (42, 'PaymentHeld', NOW(), 
   '{"amount": 1000, "paymentIntentId": "pi_...", "provider": "Stripe"}',
   'HoldFundsCommand')

-- Compute current status:
SELECT EventType FROM PaymentEvents 
WHERE TransactionId = 42 
ORDER BY Id DESC LIMIT 1
```

**Events to Capture:**

| Event | When | Payload | Use Case |
|-------|------|---------|----------|
| `TransactionCreated` | MediatR command | {client, consultant, amount, service} | Audit trail |
| `PaymentHeld` | HoldFundsCommand success | {paymentIntentId, amount, fee} | Audit trail |
| `PaymentReceivedFromWebhook` | Stripe webhook confirmed | {stripeEventId, timestamp} | Dedup tracking |
| `PaymentReleased` | ReleaseFundsCommand success | {captureId, timestamp} | Payout tracking |
| `DisputeRaised` | DisputeFundsCommand success | {reason, timestamp} | Dispute history |
| `PaymentRefunded` | Post-MVP | {refundId, amount} | Refund history |
| `WebhookRetried` | Duplicate webhook | {eventId, retryCount} | Dedup history |

**Benefits:**

- ✅ Complete audit trail (every state change recorded)
- ✅ Regulatory compliance (reconstruct timeline, prove no tampering)
- ✅ Debugging aid (see full history of a transaction)
- ✅ Append-only (no data loss; immutable log)
- ✅ Enables timeline views in dashboards

**Trade-offs:**

- ❌ More complex queries (must compute state from events)
- ❌ Storage growth (one row per event vs. one row per transaction)
- ❌ Data migration (backfill existing transactions as "created" event)

---

## 3. Outbox Pattern (v1.1) — HIGH PRIORITY

**Problem:**
- Current architecture: Save transaction → publish event via IEventBus
- If process crashes between these steps: event lost forever
- Downstream listeners (email notifications, audit dashboards) miss updates
- Inconsistency: "Transaction exists but email never sent"

**Scenario:**

```
Time 1: INSERT INTO EscrowTransaction VALUES (42, 'Held', ...)  ← SUCCESS
Time 2: eventBus.PublishAsync(PaymentReceivedEvent)             ← CRASH HERE
        Process terminates before event published
Result: Transaction in DB but email listener never notified
```

**Solution (Outbox Pattern):**

Atomically save both transaction AND outbox entry in same DB transaction:

```
BEGIN TRANSACTION
  INSERT INTO EscrowTransaction VALUES (42, 'Held', ...)
  INSERT INTO OutboxEvents VALUES 
    (null, 42, 'PaymentReceivedEvent', '{"..."}', false)
COMMIT

Background Service (every 100ms):
  SELECT * FROM OutboxEvents WHERE Published = false LIMIT 100
  FOR EACH row:
    eventBus.PublishAsync(event)
    UPDATE OutboxEvents SET Published = true WHERE Id = row.Id
```

**Benefits:**

- ✅ Event delivery guaranteed (no in-memory loss)
- ✅ Survives process crash (unpublished events retried on restart)
- ✅ Deferred publishing (DB commit first, event second)
- ✅ Enables distributed systems

**Trade-offs:**

- ❌ Background service complexity (polling, batch processing)
- ❌ Outbox table grows indefinitely (need cleanup job)
- ❌ Polling overhead (even if few events to process)

---

## 4. Saga Pattern (v1.2) — MEDIUM PRIORITY

**Problem:**
- Future feature: Multi-step business process (e.g., dispute resolution)
- Cannot express: "If dispute approved, then refund; if denied, notify consultant"
- No compensation logic: "If step 2 fails, rollback step 1"
- Current model: Single handler per command (no choreography)

**Example Use Case:**

```
User workflow:
  1. Client files dispute on transaction
  2. System queries compliance API (async)
  3. If approved: capture refund and pay consultant
  4. If denied: notify client and archive
  5. On timeout (30s): manual review queue

Current approach: No way to express this multi-step flow
Saga Pattern: Orchestrator listens to events, drives state machine
```

**Solution:**

```csharp
// SagaOrchestrator.cs
public class DisputeResolutionSaga : INotificationHandler<DisputeRaisedNotification>
{
    public async Task Handle(DisputeRaisedNotification notification, CancellationToken ct)
    {
        var sagaState = new DisputeSagaState
        {
            TransactionId = notification.TransactionId,
            Status = "AwaitingCompliance"
        };
        await sagaStateRepository.SaveAsync(sagaState);

        // Step 1: Check compliance
        var complianceResult = await complianceApi.CheckAsync(
            notification.TransactionId, 
            timeout: TimeSpan.FromSeconds(30), ct);

        if (complianceResult.Approved)
        {
            // Step 2: Approve refund
            var refundCommand = new ApproveRefundCommand 
            { 
                TransactionId = notification.TransactionId,
                Reason = "Dispute resolved"
            };
            await mediator.Send(refundCommand, ct);
            
            sagaState.Status = "Completed";
        }
        else
        {
            // Step 3: Deny refund
            var denyCommand = new DenyRefundCommand 
            { 
                TransactionId = notification.TransactionId,
                Reason = complianceResult.Reason
            };
            await mediator.Send(denyCommand, ct);
            
            sagaState.Status = "Failed";
        }

        await sagaStateRepository.SaveAsync(sagaState);
    }
}
```

**Benefits:**

- ✅ Express complex workflows
- ✅ Compensation logic (rollback on failure)
- ✅ Audit trail (saga state transitions recorded)
- ✅ Timeout handling

**Trade-offs:**

- ❌ Significant complexity (state machines, timeouts)
- ❌ Distributed transaction semantics (eventual consistency)
- ❌ Testing complexity (async flows)

---

## 5. Dead Letter Queue (v1.2–v1.3) — MEDIUM PRIORITY

**Problem:**
- Edge cases: Webhook for deleted transaction, invalid payload, encoding errors
- Current: Log warning, ignore (silent failure)
- Post-MVP: Might want to investigate (fraud signal? API version mismatch?)
- No way for ops team to identify and fix systemic issues

**Solution:**

```sql
CREATE TABLE DeadLetterQueue (
  Id INT PRIMARY KEY,
  EventType VARCHAR(100),
  Payload JSONB,
  Reason VARCHAR(500),
  CreatedAt DATETIME,
  ReviewedAt DATETIME,
  Status VARCHAR(50)  -- pending, investigated, fixed, discarded
)

-- When event can't be processed:
INSERT INTO DeadLetterQueue 
  (EventType, Payload, Reason, CreatedAt, Status)
VALUES 
  ('payment_intent.succeeded', '{"..."}', 'Transaction not found', NOW(), 'pending')
```

**Admin Workflow:**

```
Ops dashboard shows:
  [payment_intent.succeeded] Transaction not found — 5 recent occurrences
  
Ops investigates:
  "Ah, legacy transactions don't have ExternalReference set. 
   Need to backfill historical PaymentIntent IDs."
   
Fix applied → re-process items → move to "fixed"
```

**Benefits:**

- ✅ Visibility into failure patterns
- ✅ Forensics for debugging
- ✅ Regulatory evidence ("We track all events")

**Trade-offs:**

- ❌ Extra table and admin UI
- ❌ Manual resolution process

---

## 6. Event Enrichment (v1.3) — LOW PRIORITY

**Problem:**
- Current: Webhooks only include payment data (ID, amount)
- Dashboards need context (customer name, service description, dates)
- Current approach: Dashboards fetch from transaction table (N+1 queries)
- Performance: Each event publication requires additional DB lookups

**Solution:**

Enrich events at publish time:

```csharp
// Current (pre-enrichment):
await eventBus.PublishAsync(new PaymentReceivedEvent 
{
    TransactionId = 42,
    Amount = 1000
});

// Post-enrichment:
var transaction = await repository.GetByIdAsync(42);
await eventBus.PublishAsync(new EnrichedPaymentReceivedEvent 
{
    TransactionId = 42,
    Amount = 1000,
    ClientName = transaction.ClientName,
    ConsultantName = transaction.ConsultantName,
    ServiceDescription = transaction.ServiceDescription,
    CreatedAt = transaction.CreatedAt,
    CompletedAt = DateTime.UtcNow
});
```

**Benefits:**

- ✅ Subscribers don't need to fetch transaction data
- ✅ Reduces N+1 queries in dashboards
- ✅ Event contains complete context

**Trade-offs:**

- ❌ Event size increases (payload larger)
- ❌ Coupling (event now includes domain fields)

---

## 7. Circuit Breaker (v1.3+) — LOW PRIORITY

**Problem:**
- Future: Multiple payment providers (Stripe + PayPal + Crypto)
- Provider API goes down (500 errors, timeouts)
- Current: Indefinite retries, cascading timeouts, request queue explosion
- User experience: "Your payment is processing..." (actually hung)

**Solution (Polly Circuit Breaker):**

```csharp
var policy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutRejectedException>()
    .CircuitBreaker(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(60)
    );

// Usage:
try 
{
    await policy.ExecuteAsync(() => stripeApi.HoldAsync(...));
}
catch (BrokenCircuitException)
{
    // Circuit open, fail fast
    throw new PaymentProviderUnavailableException();
}
```

**States:**

```
CLOSED (normal)
  ↓ (5 failures in 60s)
OPEN (failing fast)
  ↓ (after 60s)
HALF_OPEN (test request)
  ├─ Success → CLOSED
  └─ Failure → OPEN
```

**Benefits:**

- ✅ Fail fast (don't retry indefinitely)
- ✅ Prevents cascading failures
- ✅ Automatic recovery (HALF_OPEN allows testing)

**Trade-offs:**

- ❌ Extra dependency (Polly)
- ❌ Requires monitoring to detect opens

---

## Implementation Roadmap

```
┌─────────────────────────────────────────────────────────────┐
│ NOW (MVP)                                                   │
│ Observational Webhook Handler ✅                           │
├─────────────────────────────────────────────────────────────┤
│ v1.1 (4-6 weeks post-MVP) — 3 patterns, 19 tasks           │
│ ├─ Event Deduplication (tc-12, 1 week)                      │
│ ├─ Event Sourcing (tc-13, 2 weeks)                          │
│ └─ Outbox Pattern (tc-14, 1.5 weeks)                        │
├─────────────────────────────────────────────────────────────┤
│ v1.2 (8-10 weeks post-MVP)                                 │
│ ├─ Saga Pattern (tc-15, 2 weeks)                           │
│ └─ Dead Letter Queue (tc-16, 3-4 days)                     │
├─────────────────────────────────────────────────────────────┤
│ v1.3+ (post-v1.2)                                          │
│ ├─ Event Enrichment (tc-17, 1-2 days)                      │
│ └─ Circuit Breaker (tc-18, 2-3 days)                       │
└─────────────────────────────────────────────────────────────┘
```

---

## Why This Order?

1. **Event Deduplication first** (solves immediate production problem)
   - Stripe retries are common in production
   - Blocks downstream workflows
   - Prerequisite for other patterns

2. **Event Sourcing second** (enables compliance)
   - Required for regulatory audit trails
   - Enables timeline reconstruction
   - Supports Dead Letter Queue

3. **Outbox Pattern third** (prepares for scaling)
   - Prerequisite for distributed systems
   - Guaranteed delivery foundation
   - Enables future microservices

4. **Saga & DLQ in v1.2** (complex workflows)
   - Only needed when multi-step processes arrive
   - Better to learn from real customer workflows first

5. **Enrichment & Circuit Breaker in v1.3+** (optimization)
   - Nice-to-have (not blocking)
   - Wait for performance metrics

---

## Success Criteria

### v1.1 Release Gate

- [ ] **Event Deduplication:** Zero duplicate events in 30-day production baseline
- [ ] **Event Sourcing:** 100% audit trail completeness (reconstruct any transaction timeline)
- [ ] **Outbox Pattern:** Zero event loss in chaos test (simulated crash/restart)
- [ ] **Performance:** Event lag < 1 second (99th percentile)
- [ ] **Backward Compatibility:** Status field still works; no migration required

### v1.2 Release Gate

- [ ] **Saga:** Complex workflows execute without manual intervention
- [ ] **Dead Letter Queue:** Ops can investigate and resolve webhook failures
- [ ] **Monitoring:** Saga state transitions logged with metrics

---

## Documentation Deliverables

After each pattern ships, create formal pattern documentation in `docs/platform/architecture/patterns/`:

- [ ] `event-deduplication.md` (tc-12f)
- [ ] `event-sourcing.md` (tc-13i)
- [ ] `outbox-pattern.md` (tc-14f)
- [ ] `saga-pattern.md` (tc-15 deliverable)
- [ ] `dead-letter-queue.md` (tc-16 deliverable)
- [ ] `event-enrichment.md` (tc-17 deliverable)

Each doc follows the same template as `observational-webhook-handler.md`:
- Intent, Problem, Solution
- Code examples (happy path, edge cases)
- Testing strategy
- Related patterns

---

## See Also

- [`v1.1-roadmap.md`](./v1.1-roadmap.md) — Detailed implementation plan with acceptance criteria
- [`observational-webhook-handler.md`](../platform/architecture/patterns/observational-webhook-handler.md) — MVP pattern (foundation)
- [`task-checklist.md`](./task-checklist.md) — Track D section for execution tracking

---

**Last Updated:** 2026-04-28  
**Status:** 📋 Planning (post-MVP)
