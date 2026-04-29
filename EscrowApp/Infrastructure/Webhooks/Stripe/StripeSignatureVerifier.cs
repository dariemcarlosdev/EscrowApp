using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace EscrowApp.Infrastructure.Webhooks.Stripe;

/// <summary>
/// Verifies Stripe webhook signatures to prevent spoofed events.
/// Uses the webhook signing secret from configuration (never hardcoded).
///
/// Signature Algorithm (Stripe v1):
/// 1. Parse header: t={timestamp},v1={signature}
/// 2. Create signed content: "{id}.{timestamp}.{json_body}"
/// 3. Compute HMAC-SHA256 using webhook secret
/// 4. Compare with provided signature (constant-time to prevent timing attacks)
/// 5. Reject if timestamp > 5 minutes old (prevent replay attacks)
///
/// DI Registration: builder.Services.AddSingleton&lt;StripeSignatureVerifier&gt;();
///
/// Throws StripeException on invalid signature or expired timestamp.
/// </summary>
public sealed class StripeSignatureVerifier
{
    private readonly ILogger<StripeSignatureVerifier> _logger;

    public StripeSignatureVerifier(ILogger<StripeSignatureVerifier> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifies the Stripe-Signature header against the raw request body.
    /// Returns the parsed Stripe Event if valid, throws StripeException if signature is invalid.
    ///
    /// SECURITY: Uses Stripe.EventUtility.ConstructEvent() for constant-time signature comparison.
    /// Never manually compare signatures — timing attacks can leak the secret.
    /// </summary>
    public global::Stripe.Event VerifyAndParse(
        string rawBody,
        string stripeSignatureHeader,
        string webhookSecret)
    {
        try
        {
            // Stripe.EventUtility handles:
            // - Parsing Stripe-Signature header (t={ts},v1={sig})
            // - Computing HMAC-SHA256 signature
            // - Constant-time comparison (prevents timing attacks)
            // - Timestamp validation (rejects if > 5 min old)
            var stripeEvent = EventUtility.ConstructEvent(
                rawBody,
                stripeSignatureHeader,
                webhookSecret,
                throwOnApiVersionMismatch: true); // Ensures we handle the Stripe API version we expect

            _logger.LogInformation(
                "✅ Webhook signature verified: EventId={EventId}, EventType={EventType}, EventTimestamp={CreatedUtc}",
                stripeEvent.Id,
                stripeEvent.Type,
                stripeEvent.Created);

            return stripeEvent;
        }
        catch (StripeException ex) when (ex.Message.Contains("timestamp"))
        {
            _logger.LogWarning(
                "⚠️ Webhook rejected: Timestamp outside tolerance window. This may be a replay attack or clock skew. Error={Error}",
                ex.Message);
            throw;
        }
        catch (StripeException ex) when (ex.Message.Contains("signature"))
        {
            _logger.LogWarning(
                "🔒 Webhook rejected: Invalid signature. Possible spoofed event or wrong webhook secret. Error={Error}",
                ex.Message);
            throw;
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                "❌ Webhook signature verification failed: {Error}",
                ex.Message);
            throw;
        }
    }
}
