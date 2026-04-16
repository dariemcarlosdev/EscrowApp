# Minimal Stripe Webhook Handler — #7

> Track C: Real-time payment event processing from Stripe.
> 
> Status: **Planned for #7** — MVP covers `payment_intent.succeeded` signature verification only.

## Overview

The MVP webhook handler processes **only** the `payment_intent.succeeded` event from Stripe with signature verification. This confirms that the hold operation was successful and funds are locked. More events (payment failures, disputes, cancellations) are deferred post-MVP.

## MVP Scope

### Supported Events

| Event | Action | MVP? | Status Transition | Post-MVP? |
|-------|--------|------|-------------------|-----------|
| `payment_intent.succeeded` | Confirm hold | ✅ YES | Pending → Held (verified) | — |
| `payment_intent.payment_failed` | Mark failed | ❌ NO | — | Yes (v1.1) |
| `payment_intent.canceled` | Sync cancellation | ❌ NO | — | Yes (v1.1) |
| `charge.dispute.created` | Flag disputed | ❌ NO | — | Yes (v1.1) |
| `charge.refunded` | Confirm refund | ❌ NO | — | Yes (v1.1) |

### Why `payment_intent.succeeded` Only?

1. **Lowest risk:** Only event that affects money flow (confirms hold is safe)
2. **Idempotent:** Safe to replay or receive duplicates
3. **No state changes required:** Transaction already in "Held" from synchronous HoldFundsCommand
4. **Logging only:** Webhook validates event, logs confirmation, no database changes
5. **Fail-safe:** If webhook is down, transactions still work (webhook is observational, not transactional)

## Architecture

```
Stripe servers
    │
    ▼ POST /api/webhooks/stripe
    │
    ├── StripeWebhookEndpoint (Infrastructure)
    │   ├── Read raw body (required by Stripe SDK)
    │   ├── Verify signature (StripeSignatureVerifier)
    │   └── Return 200 OK immediately (Stripe timeout: 30s)
    │
    └── (if verified) → PaymentIntentEventHandler (Application)
        ├── Deserialize event
        ├── Filter: Only process `payment_intent.succeeded`
        ├── Log confirmation
        └── Update transaction status to "Verified" (optional audit field)
```

**Key pattern:** Endpoint returns 200 OK to Stripe **immediately**, then processes event asynchronously (optional queue in v1.1).

## Implementation Checklist — #7

### Phase 7a: Endpoint Setup (Infrastructure)

```
- [ ] Create StripeWebhookEndpoint.cs
  - [ ] Minimal API endpoint at POST /api/webhooks/stripe
  - [ ] AllowAnonymous() — Stripe doesn't send API keys
  - [ ] Read raw request body (Stripe SDK requirement)
  - [ ] Inject IStripeSignatureVerifier
  
- [ ] Create StripeSignatureVerifier.cs
  - [ ] Verify Stripe-Signature header
  - [ ] HMAC-SHA256 validation
  - [ ] Timestamp check (prevents old replays)
  - [ ] Return 400 Bad Request if invalid
  - [ ] Log verification failures as security events
  
- [ ] Update Program.cs
  - [ ] Register StripeSignatureVerifier in DI
  - [ ] Map endpoint: app.MapPost("/api/webhooks/stripe", ...)
  - [ ] Get webhook secret from configuration
```

### Phase 7b: Event Handler (Application)

```
- [ ] Create PaymentIntentEventHandler.cs
  - [ ] Check event.Type == "payment_intent.succeeded"
  - [ ] Deserialize PaymentIntent from event
  - [ ] Verify external reference matches transaction
  - [ ] Log: "Payment intent confirmed, holding funds..."
  - [ ] (Optional) Update transaction.PaymentIntentVerified = true
  - [ ] (Optional) Update transaction status to "Verified"
  
- [ ] Inject IEscrowTransactionRepository
  - [ ] Fetch transaction by ExternalReference
  - [ ] Log confirmation (never return error to Stripe if tx not found)
```

### Phase 7c: Configuration

```
- [ ] Add Stripe webhook secret to appsettings.json (Development only)
  {
    "Stripe": {
      "WebhookSecret": "whsec_test_..." // Override via STRIPE__WEBHOOKSECRET env var
    }
  }
```

### Phase 7d: Testing

```
- [ ] Unit test PaymentIntentEventHandler
  - [ ] Valid event updates transaction
  - [ ] Invalid event type is ignored
  - [ ] Missing transaction doesn't throw
  
- [ ] Integration test StripeWebhookEndpoint
  - [ ] Valid signature is accepted
  - [ ] Invalid signature returns 400
  - [ ] Old timestamp is rejected
  
- [ ] Local testing with Stripe CLI
  - [ ] `stripe listen --forward-to localhost:8080/api/webhooks/stripe`
  - [ ] Trigger test event: `stripe trigger payment_intent.succeeded`
```

## Code Examples

### StripeWebhookEndpoint.cs

```csharp
namespace EscrowApp.Infrastructure.Webhooks.Stripe;

public static class StripeWebhookEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpRequest request,
        IStripeSignatureVerifier verifier,
        IMediator mediator,
        ILogger<StripeWebhookEndpoint> logger,
        CancellationToken ct = default)
    {
        try
        {
            // Read raw body (Stripe SDK requirement)
            var body = await request.Body.ReadAsStringAsync(ct);
            var signature = request.Headers["Stripe-Signature"];

            // Verify signature
            if (!verifier.VerifySignature(body, signature, out var @event))
            {
                logger.LogWarning("Invalid webhook signature from IP {IP}", request.HttpContext.Connection.RemoteIpAddress);
                return Results.BadRequest("Invalid signature");
            }

            // Dispatch to handler (fire-and-forget, return 200 OK immediately)
            _ = mediator.Publish(new StripeWebhookEvent(@event.Type, @event.Data.Object as dynamic), ct);

            // Always return 200 OK to Stripe (even if handler fails)
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook endpoint error");
            return Results.Ok(); // ← Still return 200 to prevent Stripe retries
        }
    }
}
```

### StripeSignatureVerifier.cs

```csharp
namespace EscrowApp.Infrastructure.Webhooks.Stripe;

public interface IStripeSignatureVerifier
{
    bool VerifySignature(string body, string signature, out Event @event);
}

public sealed class StripeSignatureVerifier : IStripeSignatureVerifier
{
    private readonly string _webhookSecret;

    public StripeSignatureVerifier(IOptions<StripeOptions> options)
    {
        _webhookSecret = options.Value.WebhookSecret ?? throw new ArgumentNullException(nameof(options));
    }

    public bool VerifySignature(string body, string signature, out Event @event)
    {
        @event = null!;

        try
        {
            // StripeEventUtility verifies signature and deserializes to Event
            @event = EventUtility.ConstructEvent(body, signature, _webhookSecret);
            return true;
        }
        catch (StripeException ex)
        {
            // Invalid signature, bad timestamp, etc.
            return false;
        }
    }
}
```

### PaymentIntentEventHandler (INotificationHandler)

```csharp
namespace EscrowApp.Features.Escrow.Webhooks;

public sealed class PaymentIntentEventHandler : INotificationHandler<StripeWebhookEvent>
{
    private readonly IEscrowTransactionRepository _repository;
    private readonly ILogger<PaymentIntentEventHandler> _logger;

    public PaymentIntentEventHandler(IEscrowTransactionRepository repository, ILogger<PaymentIntentEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(StripeWebhookEvent notification, CancellationToken ct)
    {
        // MVP: Only process payment_intent.succeeded
        if (notification.Type != "payment_intent.succeeded")
            return;

        try
        {
            var paymentIntent = notification.Data as PaymentIntent;
            if (paymentIntent?.Id == null)
                return;

            // Fetch transaction by external reference
            var transaction = await _repository.GetByExternalReferenceAsync(paymentIntent.Id, ct);
            
            if (transaction == null)
            {
                _logger.LogWarning("Webhook: PaymentIntent {PiId} not found in database", paymentIntent.Id);
                return; // Don't throw — webhook is observational
            }

            // Log confirmation
            _logger.LogInformation("Webhook: Payment intent {PiId} confirmed (amount: {Amount})", 
                paymentIntent.Id, 
                paymentIntent.Amount / 100m);

            // Optional: Update verification flag (idempotent)
            // transaction.PaymentIntentVerified = true;
            // await _repository.UpdateAsync(transaction, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment_intent.succeeded webhook");
            // Don't throw — Stripe retries if we throw
        }
    }
}
```

## Security Guardrails — MANDATORY

| Rule | Implementation | Why |
|---|---|---|
| **Signature verification always** | `StripeSignatureVerifier.VerifySignature()` | Prevents forged events from attacking transactions |
| **Never process unverified events** | Early return/400 if verification fails | Forgery protection |
| **Webhook secret in env vars** | `STRIPE__WEBHOOKSECRET` env var only | Never hardcode in source |
| **Return 200 OK immediately** | Stripe times out after 30s; return before processing | Stripe retries if timeout; idempotent handler prevents duplicates |
| **Never update transaction state** | Webhook is observational only (log only, no status update) | Source of truth is synchronous HoldFundsCommand; webhook confirms |
| **Timestamp validation** | EventUtility checks timestamp within 5-minute window | Prevents replay attacks (old event replayed by attacker) |
| **Log failed verifications** | Security event log with IP address | Audit trail for brute-force attempts |
| **No exception to caller** | Catch and log all exceptions; never throw to endpoint | Prevents Stripe retries from cascading errors |

## Testing Patterns

### Unit Test (EventHandler)

```csharp
[Fact]
public async Task Handle_PaymentIntentSucceeded_LogsConfirmation()
{
    // Arrange
    var repository = new Mock<IEscrowTransactionRepository>();
    var logger = new Mock<ILogger<PaymentIntentEventHandler>>();
    var handler = new PaymentIntentEventHandler(repository.Object, logger.Object);

    var transaction = new EscrowTransaction { Id = 1, ExternalReference = "pi_123" };
    repository.Setup(r => r.GetByExternalReferenceAsync("pi_123", It.IsAny<CancellationToken>()))
        .ReturnsAsync(transaction);

    var @event = new StripeWebhookEvent(
        "payment_intent.succeeded",
        new PaymentIntent { Id = "pi_123", Amount = 500000 }
    );

    // Act
    await handler.Handle(@event, CancellationToken.None);

    // Assert
    logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), 
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("confirmed")),
        It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
}

[Fact]
public async Task Handle_NonPaymentIntentEvent_Ignored()
{
    // ...
    var @event = new StripeWebhookEvent("charge.dispute.created", /* ... */);
    await handler.Handle(@event, CancellationToken.None);
    // Assert: repository.Verify() never called
}
```

### Integration Test (Endpoint)

```csharp
[Fact]
public async Task WebhookEndpoint_ValidSignature_Returns200()
{
    // Use Stripe SDK to generate valid event + signature
    var payloadJson = """{"type":"payment_intent.succeeded","data":{...}}""";
    var signature = GenerateTestSignature(payloadJson); // Use Stripe test secret

    var response = await Client.PostAsync("/api/webhooks/stripe", 
        new StringContent(payloadJson, Encoding.UTF8, "application/json"));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task WebhookEndpoint_InvalidSignature_Returns400()
{
    var badSignature = "t=123456,v1=badbadbad";
    var response = await Client.PostAsync("/api/webhooks/stripe",
        new StringContent("{}", Encoding.UTF8, "application/json"),
        new Dictionary<string, string> { ["Stripe-Signature"] = badSignature }
    );

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

## Local Testing with Stripe CLI

```bash
# Install Stripe CLI (if not already)
# https://stripe.com/docs/stripe-cli

# Start listening for events
stripe listen --forward-to localhost:8080/api/webhooks/stripe

# In another terminal, trigger a test event
stripe trigger payment_intent.succeeded

# View webhook delivery history
stripe logs tail
```

## Files to Create

| File | Purpose |
|------|---------|
| `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` | Minimal API endpoint |
| `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` | Signature verification |
| `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` | Event handler (MediatR notification) |
| `Features/Escrow/Webhooks/StripeWebhookEvent.cs` | Domain event record |
| `EscrowApp.Tests/Infrastructure/Webhooks/StripeSignatureVerifierTests.cs` | Unit tests |
| `EscrowApp.Tests/Features/Escrow/PaymentIntentEventHandlerTests.cs` | Handler tests |

## Dependencies

- `#1 Platform Fee logic` (webhook processes payments already created by HoldFundsCommand)
- Stripe SDK (already installed)
- MediatR (already configured)

## Unblocks

- ✅ Real-time payment confirmation logging
- ✅ Audit trail for successful holds
- ✅ Foundation for post-MVP dispute/cancellation webhooks

## Post-MVP Enhancements

| Feature | Trigger | Why Deferred |
|---------|---------|--------------|
| `payment_intent.payment_failed` | User submits invalid payment method | Log only; user can retry in UI |
| `charge.dispute.created` | Cardholder disputes charge | Handle in DisputeFunds workflow |
| `charge.refunded` | Support manual refunds | Rare in MVP; admin SQL queries sufficient |
| Webhook event deduplication | >100 duplicate events/day | Idempotent handler sufficient MVP |
| Dead letter queue | Webhook processing fails | Log and alert; manual recovery pre-MVP |
| Connect account webhooks | Multiple Stripe Connect accounts | Not needed for single-merchant MVP |

## Related Documentation

- [Architecture → Stripe Webhooks](../../architecture/stripe-webhooks/stripe-webhooks.md) — Full webhook architecture
- [Features → Release Funds](../../features/release-funds/release-funds.md) — Synchronous release flow
- [Deployment → Cloud](../deployment/deployment.md) — Production webhook configuration
