namespace EscrowApp.Infrastructure.Options;

/// <summary>
/// Configuration record for Stripe webhook endpoint settings.
/// Bound from configuration section "Stripe:Webhook" via IOptions{T}.
/// 
/// DI Registration: builder.Services.Configure{StripeWebhookOptions}(
///     builder.Configuration.GetSection("Stripe:Webhook"));
/// 
/// Usage in handlers:
///     public class MyHandler(IOptions{StripeWebhookOptions} options)
///     {
///         var secret = options.Value.EndpointSecret;
///     }
/// </summary>
public sealed record StripeWebhookOptions
{
    /// <summary>
    /// Stripe webhook endpoint signing secret (whsec_...).
    /// Used to verify HMAC-SHA256 signatures in Stripe-Signature headers.
    /// 
    /// Configuration path: appsettings.json → Stripe:Webhook:EndpointSecret
    /// In production, load from Key Vault or environment variables, never commit hardcoded.
    /// </summary>
    public string EndpointSecret { get; init; } = string.Empty;
}
