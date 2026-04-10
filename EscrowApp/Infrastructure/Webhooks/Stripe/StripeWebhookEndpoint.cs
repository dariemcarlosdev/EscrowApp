using Microsoft.AspNetCore.Mvc;

namespace EscrowApp.Infrastructure.Webhooks.Stripe;

/// <summary>
/// Stripe webhook endpoint — receives raw events from Stripe and dispatches
/// to domain handlers after signature verification and deduplication.
///
/// Registration: app.MapPost("/api/webhooks/stripe", StripeWebhookEndpoint.HandleAsync);
///
/// SECURITY: This endpoint must NOT require [Authorize] — Stripe sends unauthenticated
/// POST requests. Authentication is via webhook signature verification instead.
/// </summary>
public static class StripeWebhookEndpoint
{
    // TODO: Register in Program.cs:
    // app.MapPost("/api/webhooks/stripe", StripeWebhookEndpoint.HandleAsync);

    public static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        [FromServices] StripeSignatureVerifier verifier,
        CancellationToken ct)
    {
        // TODO: Implement webhook handling flow:
        // 1. Read raw request body as string
        // 2. Extract Stripe-Signature header
        // 3. Verify signature via StripeSignatureVerifier.VerifyAndParse()
        // 4. Check event type (payment_intent.succeeded, payment_intent.canceled, etc.)
        // 5. Deduplicate by event ID (store processed event IDs)
        // 6. Dispatch to appropriate MediatR handler or IEventBus
        // 7. Return 200 OK (Stripe retries on non-2xx)
        //
        // IMPORTANT: Return 200 quickly — do heavy processing async or via event bus

        throw new NotImplementedException(
            "Stripe webhook endpoint not yet implemented.");
    }
}
