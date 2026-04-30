# Post-MVP Implementation Guide & Next Steps

> How to execute the v1.1 roadmap. Bridges strategic planning to tactical execution.  
> **Audience:** Developers, tech leads, project managers  
> **When to Read:** After Track C (MVP webhooks) completes  
> **Status:** 📋 Execution guide (ready for team handoff)

---

## Quick Reference

### For Different Roles

**👨‍💻 Developers:** Go to [Implementation Sequence](#implementation-sequence)  
**🏗️ Architects:** Go to [Architecture Decisions](#architecture-decisions)  
**📊 Managers:** Go to [Timeline & Resources](#timeline--resources)  
**📋 QA/Testing:** Go to [Success Criteria](#success-criteria)

---

## Context: MVP → v1.1 Evolution

The Observational Webhook Handler (MVP) is **intentionally safe and simple**. v1.1 patterns extend it without breaking existing code:

| Concern | MVP | v1.1 | v1.2+ |
|---------|-----|------|-------|
| **State transitions** | Sync only (HoldFundsCommand) | Sync + append events | Saga orchestration |
| **Duplicates** | Accepted (no dedup) | Eliminated (tc-12) | — |
| **Audit trail** | None (status enum only) | Complete (event log) | Saga timeline |
| **Event delivery** | Best-effort (in-process) | Guaranteed (Outbox) | Multi-provider routing |
| **Complexity** | Low (straightforward) | Medium (additive) | High (conditional) |

**Key Insight:** Each v1.1 pattern adds on top; nothing requires rewriting MVP code.

---

## Implementation Sequence

### Phase 1: Event Deduplication (Week 1-2)

**Why First:** Solves immediate production risk; prerequisite for later patterns.

**What Gets Built:**

```
Webhooks table (dedup cache)
  ↓
WebhookDeduplicationService (check cache, mark processed)
  ↓
Updated StripeWebhookEndpoint (call dedup service)
  ↓
Unit tests (valid event, duplicate, expired)
  ↓
Response headers (X-Webhook-Id, X-Duplicate for ops monitoring)
```

**Tasks:**

| ID | Task | Who | Days | Acceptance Criteria |
|----|------|-----|------|-------------------|
| tc-12a | Schema: Webhooks table | Dev | 1 | Migration compiles; table has (event_id PK, status, payload, created_at) |
| tc-12b | Modify endpoint to check cache | Dev | 2 | Incoming: if event_id exists, return 204 immediately |
| tc-12c | WebhookDeduplicationService | Dev | 2 | Service: check → process (if new) → mark published |
| tc-12d | Unit tests | QA | 2 | 100% path coverage (valid, dup, expired, malformed) |
| tc-12e | Response headers | Dev | 1 | Response includes X-Webhook-Id, X-Duplicate headers |

**Success Metric:**

```
Zero duplicate PaymentReceivedEvent publications in 30-day prod baseline.
Verify with: SELECT COUNT(*), event_type FROM OutboxEvents GROUP BY event_type
Expected: Each event appears exactly once (or minimal variance for retries)
```

**Code Diff (Rough Estimate):**
- 50 lines: WebhookDeduplicationService
- 20 lines: StripeWebhookEndpoint modifications
- 80 lines: Tests

**Total: ~150 lines**

---

### Phase 2: Event Sourcing (Week 3-4)

**Why Second:** Foundation for audit compliance; enables Dead Letter Queue.

**What Gets Built:**

```
PaymentEvents table (append-only log)
  ↓
PaymentEventStore service (immutable appends)
  ↓
Computed Status property (derives from latest event)
  ↓
Update all handlers (HoldFundsHandler, ReleaseFundsHandler, etc.)
  → Append events instead of mutating Status
  ↓
Timeline UI (TransactionTimeline.razor shows event history)
  ↓
Integration tests (event append, status computation, crash recovery)
```

**Tasks:**

| ID | Task | Who | Days | Acceptance Criteria |
|----|------|-----|------|-------------------|
| tc-13a | Schema: PaymentEvents table | Dev | 1 | Migration; table has (id, transaction_id, event_type, payload, timestamp, actor) |
| tc-13b | PaymentEventStore service | Dev | 2 | AppendEventAsync() always succeeds; immutable |
| tc-13c | Status computed property | Dev | 3 | EscrowTransaction.Status returns computed value; reads still work |
| tc-13d | CreateAndHoldFundsHandler updated | Dev | 1 | On success: AppendEventAsync('PaymentHeld', {...}) |
| tc-13e | ReleaseFundsHandler updated | Dev | 1 | On success: AppendEventAsync('PaymentReleased', {...}) |
| tc-13f | DisputeFundsHandler updated | Dev | 1 | On success: AppendEventAsync('DisputeRaised', {...}) |
| tc-13g | Integration tests | QA | 2 | Event append, status computed, timeline retrieval, crash scenarios |
| tc-13h | Timeline UI component | UI Dev | 2 | TransactionTimeline.razor renders events in chronological order |

**Success Metric:**

```
100% audit trail completeness:
  1. Create transaction
  2. Verify PaymentEvents has 'TransactionCreated' event
  3. Hold funds
  4. Verify PaymentEvents has 'PaymentHeld' event
  5. Release funds
  6. Verify PaymentEvents has 'PaymentReleased' event
  
Regulatory query: Reconstruct transaction timeline
  SELECT * FROM PaymentEvents WHERE transaction_id = 42 ORDER BY id
  Result: Complete state change history with timestamps and actors
```

**Code Diff (Rough Estimate):**
- 80 lines: PaymentEventStore service
- 40 lines: EscrowTransaction.Status property
- 30 lines: Handler updates (each handler)
- 100 lines: UI component
- 150 lines: Tests

**Total: ~400+ lines**

---

### Phase 3: Outbox Pattern (Week 5-6)

**Why Third:** Prepares for distributed systems; guarantees event delivery.

**What Gets Built:**

```
OutboxEvents table (deferred publishing queue)
  ↓
OutboxPublishingService (IHostedService, background polling)
  ↓
Update all handlers (append to OutboxEvents in same transaction)
  ↓
Health check endpoint (/health/outbox shows lag)
  ↓
Chaos tests (crash scenarios, verify recovery)
```

**Tasks:**

| ID | Task | Who | Days | Acceptance Criteria |
|----|------|-----|------|-------------------|
| tc-14a | Schema: OutboxEvents table | Dev | 1 | Migration; table has (id, transaction_id, event_type, payload, published, created_at) |
| tc-14b | OutboxPublishingService | Dev | 2 | IHostedService polls every 100ms; publishes unpublished rows |
| tc-14c | Update handlers | Dev | 2 | Each handler INSERTs into OutboxEvents (same DB transaction) |
| tc-14d | Health check | Dev | 1 | GET /health/outbox returns {status: ok, lag_ms: 450, pending_count: 3} |
| tc-14e | Integration tests | QA | 2 | Process crash recovery, event published exactly once, lag monitoring |
| tc-14f | Documentation | Tech Writer | 1 | Create docs/platform/architecture/patterns/outbox-pattern.md |

**Success Metric:**

```
Zero event loss on process crash:
  1. Start OutboxPublishingService
  2. Simulate process crash (kill -9)
  3. Restart process
  4. Verify: All pending OutboxEvents published
  5. No duplicates (Published = true prevents re-publishing)
  
Performance: Event lag < 1 second (99th percentile)
  Query: SELECT MAX(EXTRACT(EPOCH FROM (now() - created_at))) 
         FROM OutboxEvents WHERE published = false
  Expected: Lag < 1000ms
```

**Code Diff (Rough Estimate):**
- 100 lines: OutboxPublishingService
- 50 lines: Handler updates (INSERTs)
- 30 lines: Health check endpoint
- 100 lines: Tests
- 100 lines: Documentation

**Total: ~380 lines**

---

## Architecture Decisions

### Decision 1: Event Deduplication Strategy

**Choice:** Database-backed (Webhooks table), not in-memory cache.

**Rationale:**
- ✅ Audit trail (every webhook attempt recorded)
- ✅ Survives process restart
- ✅ Minimal performance impact (indexed PK lookup)
- ✅ Regulatory value (prove we processed each event)
- ❌ Slight complexity (extra table)

**Alternative (Rejected):** Redis cache
- ✅ Fast
- ❌ Data loss on restart
- ❌ No audit trail

---

### Decision 2: Event Sourcing via Side-By-Side

**Choice:** Keep Status column, compute from events (no migration).

**Rationale:**
- ✅ Backward compatible (Status still works)
- ✅ No data migration (avoids downtime)
- ✅ Gradual transition (can deprecate Status later)
- ✅ Safe (if event log corrupts, Status is fallback)
- ❌ Slight inefficiency (two ways to compute state)

**Example:**

```csharp
// Old code still works:
if (transaction.Status == "Held") { ... }

// New code uses log:
var status = transaction.Status;  // Computed from latest event
```

**Migration Path (v1.2+):** Deprecate Status column, compute-only.

---

### Decision 3: Outbox Polling Interval

**Choice:** 100ms (10 polls per second).

**Rationale:**
- ✅ Acceptable lag (< 1s for 99% of events)
- ✅ Low CPU overhead (not aggressive)
- ✅ Scales to 100+ events/second (batch of 100 per poll)
- ❌ Not real-time (eventual consistency)

**Tuning:** If lag becomes issue, reduce to 50ms or implement event-driven publishing (PgBoss, RabbitMQ).

---

### Decision 4: Saga Pattern Orchestration Style

**Choice:** Choreography (event-driven), not central orchestrator.

**Rationale:**
- ✅ Loose coupling (sagas don't need to know about each other)
- ✅ Easy to add new flows (new saga handler)
- ✅ Works with MediatR notifications
- ❌ Harder to trace flow (distributed across handlers)

**Later (v1.2+):** If flows become complex, switch to central orchestrator (Akka.NET, Quartz).

---

## Timeline & Resources

### Effort Estimate

| Phase | Tasks | Dev Days | QA Days | Docs Days | Total | Team Size |
|-------|-------|----------|---------|-----------|-------|-----------|
| Dedup | tc-12a–e | 8 | 2 | 1 | **11 days** | 1 dev |
| Sourcing | tc-13a–h | 12 | 3 | 2 | **17 days** | 1-2 devs |
| Outbox | tc-14a–f | 8 | 2 | 1 | **11 days** | 1 dev |
| **Total v1.1** | **tc-12 to tc-14** | **28** | **7** | **4** | **39 days** | **2-3 devs** |

### Recommended Team Structure

```
Option A (Serial, 1 dev):
  Week 1-2: Dev A on tc-12 (dedup)
  Week 3-4: Dev A on tc-13 (sourcing)
  Week 5-6: Dev A on tc-14 (outbox)
  Total: 6 weeks, 1 full-time developer

Option B (Parallel, 2 devs):
  Week 1-2: Dev A on tc-12 (dedup)
            Dev B on planning tc-13
  Week 3-4: Dev A on tc-13 (sourcing)
            Dev B on planning tc-14
  Week 5-6: Dev A on tc-14 (outbox)
            Dev B on tests + docs
  Total: 6 weeks, 1.5-2 full-time developers (faster)

Option C (Aggressive, 3 devs):
  Week 1: Dev A on tc-12a-b (dedup, core logic)
          Dev B on tc-13a-b (sourcing, core logic)
          Dev C on planning
  Week 2: Dev A on tc-12c-e (dedup, tests)
          Dev B on tc-13c-d (sourcing, handlers)
          Dev C on tc-13e-f (sourcing, handlers)
  Week 3-4: Converge on tests + docs
  Total: 4 weeks, 2-3 full-time developers (fastest)
```

---

## Success Criteria

### v1.1 Release Gate Checklist

**Event Deduplication (tc-12):**
- [ ] Zero duplicate PaymentReceivedEvent in 30-day baseline
- [ ] Webhooks table has 100% of inbound events (even retries)
- [ ] Response latency < 50ms (cache hit)
- [ ] X-Webhook-Id header present on all responses
- [ ] Stripe CLI manual testing: send event 3x, see only 1 PaymentReceivedEvent

**Event Sourcing (tc-13):**
- [ ] PaymentEvents table has events for all transactions
- [ ] Status computed property matches last event
- [ ] Timeline view shows all state changes
- [ ] Regulatory query: reconstruct timeline for any transaction
- [ ] Unit test: status computation from events (happy path, edge cases)
- [ ] Integration test: crash recovery (events persist)

**Outbox Pattern (tc-14):**
- [ ] Zero event loss in chaos test (kill -9, restart, verify events published)
- [ ] OutboxEvents table populated correctly
- [ ] OutboxPublishingService publishes all rows
- [ ] Health check `/health/outbox` returns accurate lag
- [ ] Event lag < 1 second (99th percentile) in load test
- [ ] No duplicate event publications (Published flag prevents retries)

**Overall v1.1:**
- [ ] All code compiles (0 errors, 0 warnings)
- [ ] All tests pass (unit + integration)
- [ ] Performance benchmarks met (dedup < 50ms, outbox lag < 1s)
- [ ] Backward compatibility confirmed (Status field still works)
- [ ] Documentation complete (pattern docs created)
- [ ] Team trained (code review, walkthrough)

---

## Execution Workflow

### Week 1: tc-12 (Event Deduplication)

```
Day 1-2: Schema + service
  ├─ Create migration: Webhooks table
  ├─ Create WebhookDeduplicationService
  ├─ Unit test (basic cache logic)
  └─ Code review

Day 3: Integration
  ├─ Modify StripeWebhookEndpoint to call service
  ├─ Update response headers
  └─ Integration test with real Stripe event

Day 4-5: Testing + docs
  ├─ Full integration tests (retry scenarios)
  ├─ Manual testing with Stripe CLI
  ├─ Performance baseline (cache hit latency)
  └─ Update task-checklist.md
```

### Week 2: tc-12 Wrap + tc-13 Start

```
Day 1-2: Final tc-12 validation
  ├─ Stress test (1000 events/second)
  ├─ Memory profiling (no leak?)
  └─ Merge to main

Day 3-5: tc-13 kickoff (Event Sourcing)
  ├─ Create migration: PaymentEvents table
  ├─ Create PaymentEventStore service
  ├─ Unit test (append logic)
  └─ Code review
```

### Weeks 3-4: tc-13 (Event Sourcing)

```
Day 1-2: Computed Status property
  ├─ Update EscrowTransaction entity
  ├─ Unit test (compute status from events)
  └─ Code review

Day 3-5: Update handlers
  ├─ CreateAndHoldFundsHandler.cs
  ├─ ReleaseFundsHandler.cs
  ├─ DisputeFundsHandler.cs
  └─ Integration tests

Day 6-8: UI + tests
  ├─ TransactionTimeline.razor component
  ├─ End-to-end tests (create → hold → release, see timeline)
  ├─ Manual testing (dashboard shows timeline)
  └─ Performance test (timeline load with 1000+ events)

Day 9-10: Documentation
  ├─ Create docs/platform/architecture/patterns/event-sourcing.md
  ├─ Update task-checklist.md
  └─ Team walkthrough
```

### Weeks 5-6: tc-14 (Outbox Pattern)

```
Day 1-2: Schema + service
  ├─ Create migration: OutboxEvents table
  ├─ Create OutboxPublishingService
  ├─ Unit test (polling logic)
  └─ Code review

Day 3-4: Integration
  ├─ Update handlers to INSERT OutboxEvents
  ├─ Verify same-transaction semantics
  ├─ Integration test (happy path)
  └─ Code review

Day 5-6: Testing + monitoring
  ├─ Chaos test (kill process, verify recovery)
  ├─ Load test (1000 events/second, measure lag)
  ├─ Health check endpoint
  └─ Performance baseline

Day 7: Documentation
  ├─ Create docs/platform/architecture/patterns/outbox-pattern.md
  ├─ Update task-checklist.md
  └─ Merge to main
```

---

## Deployment Considerations

### Database Migrations

Each phase requires a migration:

```sql
-- Phase 1: Webhooks table
CREATE TABLE Webhooks (
  event_id VARCHAR(100) PRIMARY KEY,
  event_type VARCHAR(50),
  payload JSONB,
  created_at TIMESTAMP,
  processed_at TIMESTAMP,
  status VARCHAR(50)
);

-- Phase 2: PaymentEvents table
CREATE TABLE PaymentEvents (
  id BIGSERIAL PRIMARY KEY,
  transaction_id INT NOT NULL REFERENCES EscrowTransaction(Id),
  event_type VARCHAR(100),
  timestamp TIMESTAMP DEFAULT NOW(),
  payload JSONB,
  actor VARCHAR(255),
  created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX idx_payment_events_transaction_id ON PaymentEvents(transaction_id);

-- Phase 3: OutboxEvents table
CREATE TABLE OutboxEvents (
  id BIGSERIAL PRIMARY KEY,
  transaction_id INT NOT NULL REFERENCES EscrowTransaction(Id),
  event_type VARCHAR(100),
  payload JSONB,
  published BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT NOW(),
  published_at TIMESTAMP
);
CREATE INDEX idx_outbox_events_published ON OutboxEvents(published) 
WHERE published = FALSE;
```

### Backward Compatibility

✅ **Status field remains unchanged:**
- Old queries: `WHERE Status = 'Held'` still work
- New approach: Status computed from events (internally)
- Migration path: Deprecate Status column in v1.2+

### Zero-Downtime Deployment

Each phase can be deployed independently:

1. **Deploy tc-12:** Webhooks table exists, but dedup logic optional
   - ✅ Old endpoint still works (no calls to dedup service)
   - ✅ Can toggle dedup on/off with feature flag

2. **Deploy tc-13:** PaymentEvents table exists, handlers append events
   - ✅ Status still works (computed falls back to column)
   - ✅ Backward compatible

3. **Deploy tc-14:** OutboxEvents table, background service polling
   - ✅ Both delivery paths work (IEventBus in-process + Outbox)
   - ✅ Can disable polling if needed

---

## Rollback Strategy

**If deployment fails:**

### tc-12 (Dedup) Rollback
```sql
-- Drop dedup logic, keep existing Webhooks table (for audit)
UPDATE StripeWebhookEndpoint SET UseDedup = false;
-- Old behavior: No cache check, accept duplicates (MVP-like)
```

### tc-13 (Sourcing) Rollback
```sql
-- Status column used instead of computed property
UPDATE EscrowTransaction.Status = 'ComputedStatus'; -- Fallback
```

### tc-14 (Outbox) Rollback
```sql
-- Disable OutboxPublishingService, use in-process IEventBus
UPDATE Program.cs: DisableHostedService(OutboxPublishingService);
```

---

## Common Pitfalls & Prevention

| Pitfall | Prevention | Test |
|---------|-----------|------|
| Dedup cache doesn't persist | Use Webhooks table (DB-backed) | Restart service, replay event |
| Status computed property breaks queries | Add index on computed Status | Query perf test |
| Event log grows unbounded | Add cleanup job (delete events > 90 days) | Monitor disk usage |
| OutboxPublishingService falls behind | Increase batch size or polling frequency | Load test (1000 events/sec) |
| Backward compatibility broken | Status column always populated | Old code still works |

---

## References

- 📄 **Pattern Analysis:** `docs/planning/post-mvp/post-mvp-patterns-analysis.md`
- 📋 **Roadmap:** `docs/planning/post-mvp/v1.1-roadmap.md`
- 📊 **Checklist:** `docs/planning/task-checklist.md` (Track D section)
- 🎯 **MVP Pattern:** `docs/platform/architecture/patterns/observational-webhook-handler.md`

---

**Last Updated:** 2026-04-28  
**Status:** 📋 Ready for Team Handoff (after Track C complete)
