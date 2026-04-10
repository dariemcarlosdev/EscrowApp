namespace EscrowApp.Infrastructure.Webhooks.Stripe;

/// <summary>
/// Verifies Stripe webhook signatures to prevent spoofed events.
/// Uses the webhook signing secret from configuration (never hardcoded).
///
/// DI Registration: builder.Services.AddSingleton&lt;StripeSignatureVerifier&gt;();
/// </summary>
public sealed class StripeSignatureVerifier
{
    // TODO: Inject webhook secret via IOptions<StripeWebhookSettings>
    // private readonly string _webhookSecret;

    /// <summary>
    /// Verifies the Stripe-Signature header against the raw request body.
    /// Returns the parsed Stripe Event if valid, throws if signature is invalid.
    /// </summary>
    public global::Stripe.Event VerifyAndParse(string rawBody, string stripeSignatureHeader)
    {
        // TODO: Implement signature verification:
        // 1. Use Stripe.EventUtility.ConstructEvent(rawBody, stripeSignatureHeader, _webhookSecret)
        // 2. Catch StripeException for invalid signatures
        // 3. Log verification result (never log the raw body — may contain PII)
        // 4. Return the parsed Event

        throw new NotImplementedException(
            "Stripe signature verification not yet implemented.");
    }
}
