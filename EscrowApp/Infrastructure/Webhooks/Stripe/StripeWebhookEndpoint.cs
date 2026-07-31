using EscrowApp.Infrastructure.Options;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace EscrowApp.Infrastructure.Webhooks.Stripe;

/// <summary>
/// Stripe webhook endpoint — receives raw events from Stripe and dispatches
/// to domain handlers after signature verification.
///
/// Routes:
/// - GET /api/webhooks/stripe (development-only diagnostic)
/// - POST /api/webhooks/stripe (Stripe callback)
/// Returns: 204 NoContent on success, RFC 7807 error response on failure
///
/// SECURITY: This endpoint does NOT require [Authorize] — Stripe sends unauthenticated
/// POST requests. Authentication is via webhook signature verification only.
/// 
/// PERFORMANCE: Returns 204 immediately after basic validation. Heavy processing
/// (database updates, event publishing) happens asynchronously via MediatR notifications.
///
/// Registration: app.MapPost("/api/webhooks/stripe", StripeWebhookEndpoint.HandleAsync);
/// </summary>
public static class StripeWebhookEndpoint
{
    /// <summary>
    /// Lightweight diagnostic response for manual local checks.
    /// Confirms the route exists without pretending that GET is a valid Stripe delivery.
    /// </summary>
    public static IResult HandleStatus()
    {
        var response = new StripeWebhookStatusResponse(
            Endpoint: "/api/webhooks/stripe",
            AcceptedMethod: "POST",
            Status: "ready",
            Message: "Stripe webhooks are accepted on POST only. Use Stripe CLI or another HTTP client to send a signed POST request to this route.");

        return TypedResults.Ok(response);
    }

    /// <summary>
    /// Main webhook handler. Validates signature, deduplicates by event ID,
    /// and dispatches to the MediatR notification pipeline.
    /// </summary>
    public static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        [FromServices] StripeSignatureVerifier verifier,
        [FromServices] IOptions<StripeWebhookOptions> webhookOptions,
        [FromServices] IPublisher mediator,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        // Non-generic ILogger is NOT registered in DI by default — only ILogger<T> and ILoggerFactory.
        // Resolving ILogger directly throws InvalidOperationException, which ApiExceptionMiddleware
        // would translate to 422. Use the factory to create a category-named logger instead.
        var logger = loggerFactory.CreateLogger(nameof(StripeWebhookEndpoint));
        try
        {
            // Step 1: Read raw body as string (required for signature verification)
            var rawBody = await ReadRawBodyAsync(httpContext.Request, ct);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                logger.LogWarning("❌ Webhook rejected: Empty request body");
                return Results.BadRequest("Request body cannot be empty");
            }

            // Step 2: Extract Stripe-Signature header (required for verification)
            if (!httpContext.Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
            {
                logger.LogWarning("❌ Webhook rejected: Missing Stripe-Signature header");
                return Results.BadRequest("Missing Stripe-Signature header");
            }

            // Step 3: Verify signature against webhook secret
            // StripeSignatureVerifier uses Stripe.EventUtility.ConstructEvent for constant-time comparison
            var stripeEvent = verifier.VerifyAndParse(
                rawBody,
                signatureHeader.ToString(),
                webhookOptions.Value.EndpointSecret);

            logger.LogInformation(
                "✅ Webhook verified and parsed: EventId={EventId}, EventType={EventType}",
                stripeEvent.Id,
                stripeEvent.Type);

            // Step 4: Dispatch to appropriate handler based on event type
            // Currently only processes payment_intent.succeeded; others are logged and ignored
            await DispatchEventAsync(stripeEvent, mediator, logger, ct);

            // Step 5: Return 204 NoContent (per Stripe webhook requirements)
            // Stripe considers 2xx responses as successful. We return 204 (no content)
            // because we don't have meaningful response data to return.
            return Results.NoContent();
        }
        catch (StripeException ex)
        {
            // Signature verification failed — return 401 Unauthorized
            // Stripe will retry the webhook on non-2xx responses
            logger.LogError(
                "🔒 Webhook security failure: {Error}",
                ex.Message);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            // Unexpected error — return 500 Internal Server Error
            // Stripe will retry the webhook
            logger.LogError(
                ex,
                "❌ Unexpected error processing webhook: {Error}",
                ex.Message);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Reads the raw request body as a string. This is required for Stripe signature verification,
    /// which uses the exact raw bytes sent by Stripe.
    ///
    /// IMPORTANT: Must read the body before any other code that might consume it.
    /// HttpRequest.Body can only be read once by default.
    /// </summary>
    private static async Task<string> ReadRawBodyAsync(HttpRequest request, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync(ct);
    }

    /// <summary>
    /// Dispatches the Stripe event to the appropriate MediatR handler.
    /// Currently only handles payment_intent.succeeded; other events are logged but ignored.
    /// </summary>
    private static async Task DispatchEventAsync(
        global::Stripe.Event stripeEvent,
        IPublisher mediator,
        ILogger logger,
        CancellationToken ct)
    {
        // Only process payment_intent.succeeded in MVP phase
        // Other events (payment_intent.payment_failed, charge.refunded, etc.) are logged but ignored
        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            try
            {
                var paymentIntent = (PaymentIntent)stripeEvent.Data.Object;

                logger.LogInformation(
                    "📨 Dispatching PaymentIntentSucceeded to MediatR: PaymentIntentId={PaymentIntentId}",
                    paymentIntent.Id);

                // Publish via MediatR — handlers are auto-discovered and executed in-process
                // Notification handlers implement INotificationHandler<PaymentIntentSucceededNotification>
                var notification = new PaymentIntentSucceededNotification(
                    PaymentIntentId: paymentIntent.Id,
                    Amount: paymentIntent.Amount,
                    Currency: paymentIntent.Currency,
                    StripeEventId: stripeEvent.Id);

                await mediator.Publish(notification, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "❌ Failed to dispatch PaymentIntentSucceeded notification: {Error}",
                    ex.Message);
                throw; // Re-throw so caller can log and return error
            }
        }
        else
        {
            // Other Stripe events are logged but ignored (post-MVP implementation)
            logger.LogInformation(
                "ℹ️ Webhook event type not yet implemented: {EventType}",
                stripeEvent.Type);
        }
    }
}

/// <summary>
/// MediatR notification triggered by the webhook endpoint when a payment_intent.succeeded event is received.
/// Handlers implement INotificationHandler&lt;PaymentIntentSucceededNotification&gt; to update transaction status,
/// publish domain events, and trigger downstream workflows.
/// </summary>
public sealed record PaymentIntentSucceededNotification(
    string PaymentIntentId,
    long Amount,
    string Currency,
    string StripeEventId) : INotification;

public sealed record StripeWebhookStatusResponse(
    string Endpoint,
    string AcceptedMethod,
    string Status,
    string Message);
