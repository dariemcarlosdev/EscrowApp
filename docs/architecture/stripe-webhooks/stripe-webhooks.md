# 12 — Stripe Webhooks

> Real-time event processing from Stripe for payment lifecycle management.

## Overview

Stripe Webhooks provide **server-to-server notifications** when payment events occur outside the application's direct control — successful charges, failed payments, disputes opened by cardholders, and refund completions. The EscrowApp must process these events to keep transaction state synchronized with Stripe's source of truth.

## Architecture

The webhook system is split between **Infrastructure** (transport/verification) and **Application** (business logic):

```
Stripe servers
    │
    ▼ POST /api/webhooks/stripe
    │
Infrastructure/Webhooks/Stripe/
├── StripeWebhookEndpoint.cs       ← Minimal API endpoint (transport)
└── StripeSignatureVerifier.cs     ← HMAC signature verification (security)
    │
    ▼ Verified Event
    │
Features/Escrow/Webhooks/
└── PaymentIntentEventHandler.cs   ← Business logic (state transitions)
```

**Why the split?**
- **SRP:** Transport concerns (HTTP, headers, raw body) are separate from business logic
- **Testability:** Business logic handler can be unit-tested without HTTP plumbing
- **Security:** Signature verification happens before any business logic executes

## Webhook Flow

```
1. Stripe sends POST to /api/webhooks/stripe
2. StripeWebhookEndpoint reads raw body + Stripe-Signature header
3. StripeSignatureVerifier validates HMAC-SHA256 signature using webhook secret
4. If invalid → 400 Bad Request (logged as security event)
5. If valid → deserialize to Stripe Event object
6. Route to PaymentIntentEventHandler based on event type
7. Handler updates EscrowTransaction state via repository
8. Return 200 OK to Stripe (must respond within 30 seconds)
```

## Supported Events

| Stripe Event | Action | State Transition |
|---|---|---|
| `payment_intent.succeeded` | Confirm hold is active | Verify "Funded (Held)" |
| `payment_intent.payment_failed` | Mark as failed | → "Payment Failed" (new status TBD) |
| `payment_intent.canceled` | Sync external cancellation | → "Cancelled" |
| `charge.dispute.created` | Flag as externally disputed | → "Disputed" |
| `charge.dispute.closed` | Update dispute resolution | Depends on outcome |
| `charge.refunded` | Confirm refund completed | → "Refunded" (future status) |

## Security

### Signature Verification (Critical)

Every webhook request MUST be verified before processing:

```csharp
// StripeSignatureVerifier validates:
// 1. Stripe-Signature header present
// 2. HMAC-SHA256 matches using webhook secret
// 3. Timestamp within tolerance (prevents replay attacks)
```

- **Never process an unverified webhook** — an attacker could forge state transitions
- Webhook secret stored in environment variable / Key Vault — never in source code
- Log verification failures as security events with IP address (but never log the secret)

### Endpoint Security

- Webhook endpoint does **NOT** use `[Authorize]` — Stripe can't send API keys
- Instead, security is provided by HMAC signature verification
- Rate limiting recommended to mitigate abuse

## Configuration

```jsonc
// appsettings.json (Development only — use env vars in production)
{
  "Stripe": {
    "WebhookSecret": "whsec_..." // ⚠️ Use env var STRIPE__WEBHOOKSECRET in production
  }
}
```

### Registration in Program.cs

```csharp
// Map the webhook endpoint (add to Program.cs)
app.MapPost("/api/webhooks/stripe", StripeWebhookEndpoint.HandleAsync)
   .AllowAnonymous(); // Auth via signature verification, not API key
```

## Phase 1: Infrastructure Implementation ✅ COMPLETE

**Status:** Implemented and compiling (tc-1, tc-2, tc-3) — 2026-04-28

### StripeWebhookOptions.cs (tc-1)

Configuration record for binding webhook endpoint secret from `appsettings.json`:

```csharp
namespace EscrowApp.Infrastructure.Options;

public sealed record StripeWebhookOptions
{
    /// <summary>
    /// Stripe webhook endpoint signing secret (whsec_...).
    /// Bound from Stripe:Webhook:EndpointSecret configuration.
    /// </summary>
    public string EndpointSecret { get; init; } = string.Empty;
}
```

**DI Registration (in Program.cs):**
```csharp
builder.Services.Configure<StripeWebhookOptions>(
    builder.Configuration.GetSection("Stripe:Webhook"));
```

**appsettings.json:**
```jsonc
{
  "Stripe": {
    "Webhook": {
      "EndpointSecret": "whsec_test_secret_here"  // Override via env var in production
    }
  }
}
```

### StripeSignatureVerifier.cs (tc-2)

HMAC-SHA256 signature verification using Stripe.EventUtility for constant-time comparison:

```csharp
namespace EscrowApp.Infrastructure.Webhooks.Stripe;

public sealed class StripeSignatureVerifier
{
    private readonly ILogger<StripeSignatureVerifier> _logger;

    public StripeSignatureVerifier(ILogger<StripeSignatureVerifier> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifies Stripe webhook signature and returns parsed event.
    /// Throws StripeException on invalid signature or expired timestamp.
    /// </summary>
    public global::Stripe.Event VerifyAndParse(
        string rawBody,
        string stripeSignatureHeader,
        string webhookSecret)
    {
        // Uses Stripe.EventUtility.ConstructEvent:
        // - Parses Stripe-Signature header (t={timestamp},v1={signature})
        // - Computes HMAC-SHA256 signature
        // - Constant-time comparison (prevents timing attacks)
        // - Rejects if timestamp > 5 minutes old (replay attack prevention)
        
        var stripeEvent = EventUtility.ConstructEvent(
            rawBody,
            stripeSignatureHeader,
            webhookSecret,
            throwOnApiVersionMismatch: true);

        _logger.LogInformation(
            "✅ Webhook signature verified: EventId={EventId}, EventType={EventType}",
            stripeEvent.Id,
            stripeEvent.Type);

        return stripeEvent;
    }
}
```

**Error Handling:**
- `StripeException` with "timestamp" → Signature outside tolerance window (possible replay attack)
- `StripeException` with "signature" → Invalid signature (possible spoofed event)
- `StripeException` (other) → Unexpected error

### StripeWebhookEndpoint.cs (tc-3)

Minimal API endpoint receiving raw Stripe events and dispatching via MediatR:

```csharp
namespace EscrowApp.Infrastructure.Webhooks.Stripe;

public static class StripeWebhookEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        [FromServices] StripeSignatureVerifier verifier,
        [FromServices] IOptions<StripeWebhookOptions> webhookOptions,
        [FromServices] IPublisher mediator,
        [FromServices] ILogger logger,
        CancellationToken ct)
    {
        // 1. Read raw body (required for signature verification)
        var rawBody = await ReadRawBodyAsync(httpContext.Request, ct);
        
        // 2. Extract Stripe-Signature header
        if (!httpContext.Request.Headers.TryGetValue("Stripe-Signature", out var sig))
            return Results.BadRequest("Missing Stripe-Signature header");

        // 3. Verify signature
        var stripeEvent = verifier.VerifyAndParse(
            rawBody,
            sig.ToString(),
            webhookOptions.Value.EndpointSecret);

        // 4. Dispatch to MediatR
        await DispatchEventAsync(stripeEvent, mediator, logger, ct);

        // 5. Return 204 NoContent (Stripe expects 2xx)
        return Results.NoContent();
    }
}
```

**MediatR Notification (defined in endpoint module):**

```csharp
public sealed record PaymentIntentSucceededNotification(
    string PaymentIntentId,
    long Amount,
    string Currency,
    string StripeEventId) : INotification;
```

**Endpoint Registration (in Program.cs):**
```csharp
// Add after other endpoint mappings
app.MapPost("/api/webhooks/stripe", StripeWebhookEndpoint.HandleAsync)
   .WithName("StripeWebhook")
   .WithOpenApi()
   .Produces(StatusCodes.Status204NoContent)
   .Produces(StatusCodes.Status400BadRequest)
   .Produces(StatusCodes.Status401Unauthorized)
   .Produces(StatusCodes.Status500InternalServerError)
   .AllowAnonymous(); // Auth via signature verification, not API key
```

**Response Codes:**
- `204 NoContent` → Webhook verified and processed successfully
- `400 Bad Request` → Missing body or header
- `401 Unauthorized` → Invalid signature
- `500 Internal Server Error` → Unexpected error (Stripe will retry)

## Files

| File | Layer | Purpose |
|------|-------|---------|
| `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` | Infrastructure | HTTP endpoint, raw body reading |
| `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` | Infrastructure | HMAC signature validation |
| `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` | Application | Business logic for payment events |

## Testing Strategy

- **Unit test** `PaymentIntentEventHandler` with mocked repository — verify state transitions
- **Integration test** `StripeWebhookEndpoint` with test webhook secret and known-good signatures
- Use Stripe CLI for local development: `stripe listen --forward-to localhost:8080/api/webhooks/stripe`

## Future Enhancements

- [ ] Webhook event deduplication (store processed event IDs)
- [ ] Dead letter queue for failed webhook processing
- [ ] Webhook event replay endpoint for missed events
- [ ] Support for Connect account webhooks (when Stripe Connect is added)
