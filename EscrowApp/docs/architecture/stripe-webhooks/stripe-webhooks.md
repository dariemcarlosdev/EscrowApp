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
