# Observational Webhook Handler Pattern

**Category:** Behavioral Pattern  
**Intent:** Handle external async events (webhooks) without mutating critical business state  
**Use Case:** Payment provider callbacks (Stripe, PayPal, blockchain indexers)  
**Complexity:** Medium  
**Status:** ✅ Implemented in Track C — Stripe webhook integration

---

## Problem

External payment providers send async webhooks to confirm operations that happened on their infrastructure. A naive implementation transitions application state based on these webhooks:

```csharp
// ❌ PROBLEMATIC: Webhook drives state machine
public async Task HandlePaymentSucceededAsync(string paymentIntentId)
{
    var transaction = await repo.GetByExternalReferenceAsync(paymentIntentId);
    
    // State transition triggered by webhook (risky!)
    transaction.Status = "Released";  // Driven by async event
    await repo.UpdateAsync(transaction);
}
```

**Risks:**
1. **Webhook retries** — Stripe retries failed webhooks; duplicate transitions corrupt state
2. **Timing issues** — Webhook might arrive before synchronous operation completes
3. **Replay attacks** — Attacker forges webhook event, triggers state change
4. **Deduplication complexity** — Must track processed event IDs to prevent duplicates
5. **Testing nightmare** — State can change via two paths (sync command + async webhook)

---

## Solution

**Core principle:** Webhooks are **observational only**. They confirm operations that already happened synchronously. Webhooks **never drive state transitions** — they publish domain events for downstream listeners.

```csharp
// ✅ SAFE: Webhook confirms, doesn't transition
public async Task Handle(PaymentIntentSucceededNotification notification, CancellationToken ct)
{
    try
    {
        var transaction = await repo.GetByExternalReferenceAsync(
            notification.PaymentIntentId, ct);

        if (transaction is null)
        {
            logger.LogWarning("Webhook for unknown PaymentIntent: {Id}", 
                notification.PaymentIntentId);
            return;  // Don't throw — webhook must succeed
        }

        // Verify existing state (webhook confirms, doesn't drive)
        if (transaction.Status != "Held" && transaction.Status != "Pending")
        {
            logger.LogWarning("Payment confirmed but status unexpected: {Status}", 
                transaction.Status);
            return;  // Ignore
        }

        // Publish event for downstream listeners (email, audit trail, dashboards)
        await eventBus.PublishAsync(new PaymentReceivedEvent
        {
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            ExternalReference = transaction.ExternalReference,
            Provider = transaction.ExternalProvider,
            // ... audit fields
        }, ct);

        logger.LogInformation("✅ Payment confirmed: {Id}", transaction.Id);
    }
    catch (Exception ex)
    {
        // Log errors but never throw (prevents webhook endpoint returning 5xx)
        logger.LogError(ex, "Webhook processing error");
    }
}
```

---

## Participants

| Role | Responsibility |
|------|-----------------|
| **Webhook Endpoint** (Infrastructure) | Receive HTTP POST, verify signature, dispatch to handler |
| **Webhook Handler** (Application) | Correlate external ID to domain entity, validate, publish event |
| **Domain Event** (Domain) | Immutable record of external confirmation for audit trail |
| **Event Bus** (Infrastructure) | Publish events to subscribers (emails, dashboards, future notifications) |

---

## Structure

```
Stripe Infrastructure
        │
        ▼ HTTP POST /api/webhooks/stripe
        │ (payment_intent.succeeded)
        │
StripeWebhookEndpoint (Infrastructure)
├─ Read raw body
├─ Verify HMAC signature (constant-time comparison)
└─ Extract Stripe-Signature header
        │
        ▼ If signature valid
        │
PaymentIntentEventHandler (Application / MediatR)
├─ Find EscrowTransaction by ExternalReference (Stripe PaymentIntent ID)
├─ Validate: transaction exists, status is "Held", amount matches
├─ Publish PaymentReceivedEvent (no status change)
└─ Log confirmation (structured, no secrets)
        │
        ▼ Never throws
        │
Return 204 NoContent to Stripe
        │
        ▼ Async
        │
Downstream event subscribers
├─ Send confirmation email
├─ Update consultant dashboard
└─ Audit trail log
```

---

## Consequences

### ✅ Advantages

1. **Safe & Idempotent:** Webhook can be replayed/retried without side effects
2. **Fail-Safe:** If webhook is down, business logic still works (state driven synchronously)
3. **Timing-Proof:** Doesn't matter when webhook arrives — it confirms, not drives
4. **No Deduplication Overhead:** Don't need event ID cache (webhook is observational)
5. **Clear Separation:** Sync = critical state, Async = confirmation + side effects
6. **Testable:** Business logic tests don't depend on webhook simulator

### ❌ Trade-offs

1. **Dual State Management:** Must maintain two paths (sync for core, async for confirmation)
2. **Eventual Consistency:** Downstream listeners see events slightly delayed
3. **More Code:** Two entry points for same domain change (command + webhook)
4. **Debugging Complexity:** Status changes from sync path, events from async path

---

## Applicability

**Use this pattern when:**
- ✅ External system owns ground truth (Stripe holds funds, not you)
- ✅ Webhook is **confirmation**, not command (payment already held, webhook confirms it)
- ✅ State transitions must be synchronous and immediate
- ✅ Webhook retries/replays are expected

**Avoid when:**
- ❌ Webhook is the only source of truth (no sync operation exists)
- ❌ State change is urgent and can't wait for confirmation
- ❌ Deduplication is trivial (single-delivery guarantee)

---

## Implementation Reference

**Real Implementation:** See [`docs/platform/architecture/stripe-webhooks/minimal-webhook-handler-mvp.md`](../stripe-webhooks/minimal-webhook-handler-mvp.md)

**Code Files:**
- `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` — HTTP transport layer
- `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` — Signature validation
- `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` — Observation logic
- `Events/PaymentReceivedEvent.cs` — Domain event published by handler

---

## Code Examples

### Example 1: Happy Path

```csharp
// Synchronous: HoldFundsCommand transitions Pending → Held
await mediator.Send(new HoldFundsCommand
{
    ClientEmail = "client@example.com",
    Amount = 1000m,
});
// Status is now "Held"

// Later, Stripe sends webhook (payment_intent.succeeded)
// Handler receives PaymentIntentSucceededNotification
await handler.Handle(notification, cancellationToken);
// Status stays "Held" (webhook is observational)
// PaymentReceivedEvent published for audit trail
```

### Example 2: Webhook Deduplication (Built-in)

```csharp
// Stripe retries webhook (network timeout)
// Second POST to /api/webhooks/stripe with same PaymentIntent ID

// Endpoint returns 204 immediately both times
// Handler processes both times:

// First webhook:
// ✓ Transaction found, status is "Held"
// ✓ PaymentReceivedEvent published

// Second webhook (retry):
// ✓ Transaction found, status is "Held" (unchanged)
// ✓ PaymentReceivedEvent published again (idempotent)
// Result: Safe, no data corruption
```

### Example 3: Transaction Not Found (Safe)

```csharp
// Webhook arrives for PaymentIntent not in database
// (e.g., created in different environment, or timing issue)

var transaction = await repo.GetByExternalReferenceAsync(paymentIntentId);
if (transaction is null)
{
    logger.LogWarning("Webhook for unknown PaymentIntent");
    return;  // Don't throw — webhook endpoint returns 204 anyway
}
// Handler never fails the webhook
```

### Example 4: State Mismatch (Safe)

```csharp
// Webhook arrives but transaction is in unexpected state
// (e.g., already released, or cancelled)

if (transaction.Status != "Held" && transaction.Status != "Pending")
{
    logger.LogWarning("Payment confirmed but status unexpected: {Status}", 
        transaction.Status);
    return;  // Ignore webhook, status doesn't change
}
```

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task Handle_WithValidTransaction_PublishesEvent()
{
    // Arrange
    var transaction = new EscrowTransaction 
    { 
        Id = 1, 
        Status = "Held",
        ExternalReference = "pi_123",
        Amount = 1000m
    };
    repoMock.Setup(r => r.GetByExternalReferenceAsync("pi_123", It.IsAny<CancellationToken>()))
        .ReturnsAsync(transaction);
    
    var notification = new PaymentIntentSucceededNotification("pi_123", 100000, "usd", "evt_123");

    // Act
    await handler.Handle(notification, cancellationToken);

    // Assert
    eventBusMock.Verify(e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), It.IsAny<CancellationToken>()), 
        Times.Once);
}

[Fact]
public async Task Handle_WithMissingTransaction_LogsWarningAndReturns()
{
    // Arrange
    repoMock.Setup(r => r.GetByExternalReferenceAsync("unknown", It.IsAny<CancellationToken>()))
        .ReturnsAsync((EscrowTransaction?)null);
    
    var notification = new PaymentIntentSucceededNotification("unknown", 100000, "usd", "evt_123");

    // Act & Assert (no exception)
    await handler.Handle(notification, cancellationToken);
    
    loggerMock.Verify(l => l.LogWarning(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
}
```

### Integration Tests

```csharp
[Fact]
public async Task Webhook_FullFlow_SignatureVerified()
{
    // Arrange
    var paymentIntentId = "pi_123";
    var amount = 100000L;  // Cents
    var secret = "whsec_test_secret";

    // Create test transaction
    var transaction = new EscrowTransaction 
    { 
        Status = "Held",
        ExternalReference = paymentIntentId,
        Amount = amount / 100m  // Convert to dollars
    };
    await db.Transactions.AddAsync(transaction);
    await db.SaveChangesAsync();

    // Simulate Stripe webhook
    var body = JsonSerializer.Serialize(new { data = new { object = new { id = paymentIntentId, amount } } });
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var signature = ComputeSignature(body, secret, timestamp);

    // Act
    var response = await client.PostAsync("/api/webhooks/stripe", 
        new StringContent(body, Encoding.UTF8, "application/json")
        {
            Headers = { { "Stripe-Signature", $"t={timestamp},v1={signature}" } }
        });

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    
    var updatedTx = await db.Transactions.FirstAsync(t => t.Id == transaction.Id);
    Assert.Equal("Held", updatedTx.Status);  // Status unchanged (observational)
}
```

---

## Post-MVP Extensions

The Observational Webhook Handler is the **foundation** for more sophisticated webhook patterns. As the platform matures, these patterns address production complexity:

### v1.1 — Event Reliability & Audit

| Pattern | Problem Solved | Complexity | Tracked As |
|---------|---|---|---|
| **Event Deduplication** | Stripe retries cause duplicate `PaymentReceivedEvent` | Medium | tc-12 |
| **Event Sourcing** | Can't reconstruct transaction timeline for audits | High | tc-13 |
| **Outbox Pattern** | Event loss if process crashes after DB commit | Medium | tc-14 |

**Recommended Approach:**
- v1.1 starts with **Event Deduplication** (database-backed, prevents duplicates)
- Extends to **Event Sourcing** (append-only log, audit trail)
- Adds **Outbox Pattern** (guaranteed delivery)

**See:** `docs/planning/v1.1-roadmap.md` for detailed implementation sequence.

### v1.2+ — Advanced Workflows

| Pattern | Use Case | Tracked As |
|---------|----------|-----------|
| **Saga Pattern** | Multi-step workflows (e.g., dispute resolution with compensation) | tc-15 |
| **Dead Letter Queue** | Investigation/forensics for unprocessable events | tc-16 |
| **Circuit Breaker** | Resilience when payment providers fail | tc-17 |

**Post-MVP Learning:** These patterns are **NOT** needed for MVP but should be **documented and planned** to guide architecture decisions post-launch.

---

## Related Patterns

- **Event Sourcing** — Alternative: store all events, rebuild state from log (more complex, better audit trail) → See v1.1 roadmap
- **Event Deduplication** — Companion: ensure single processing of each webhook event → See v1.1 roadmap
- **Outbox Pattern** — Companion: guarantee delivery of domain events → See v1.1 roadmap
- **Event Bus** — Always use for publishing domain events (enables decoupling)
- **Idempotency** — Webhook retries are idempotent (safe to replay)
- **CQRS** — MediatR commands (sync) vs. notifications (async)

---

## References

- **Book:** "Domain-Driven Design" by Eric Evans — Event sourcing, bounded contexts
- **Article:** "Webhooks best practices" — Stripe, PayPal documentation
- **Pattern Catalog:** "Enterprise Integration Patterns" — Competing consumers, deduplication

---

## See Also

- [`stripe-webhooks.md`](../stripe-webhooks/stripe-webhooks.md) — Architecture overview
- [`minimal-webhook-handler-mvp.md`](../stripe-webhooks/minimal-webhook-handler-mvp.md) — Stripe implementation spec
- [`event-bus.md`](../event-bus/event-bus.md) — Domain event publishing
- [`testing-strategy.md`](../../system/testing/testing-strategy.md) — Test patterns
