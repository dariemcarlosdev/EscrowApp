using Microsoft.Extensions.Configuration;
using Stripe;

namespace EscrowApp.Services.Strategies;

/// <summary>
/// Stripe strategy: implements hold, release, AND cancel capabilities.
/// Knows NOTHING about EscrowDbContext — pure Stripe SDK orchestration only.
/// Idempotency keys prevent double-actions on network retries (§4).
/// Uses DI-injected PaymentIntentService for HTTP connection pooling.
/// </summary>
public sealed class StripePaymentStrategy(
    PaymentIntentService paymentIntentService,
    IConfiguration configuration)
    : IEscrowPaymentStrategy, IFundHoldable, IFundReleasable, IFundCancellable
{
    public string ProviderName => "Stripe";

    public async Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId, string idempotencyKey, CancellationToken ct = default)
    {
        var returnUrl = configuration["Stripe:PaymentReturnUrl"]
            ?? throw new InvalidOperationException("Stripe:PaymentReturnUrl is not configured.");

        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = "usd",
            PaymentMethod = sourcePaymentMethodId,
            CaptureMethod = "manual",
            Confirm = true,
            ReturnUrl = returnUrl
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        PaymentIntent intent = await paymentIntentService.CreateAsync(options, requestOptions, ct);
        return intent.Id;
    }

    public async Task<bool> ReleaseFundsAsync(string externalReference, string idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(externalReference)) return false;

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        PaymentIntent intent = await paymentIntentService.CaptureAsync(externalReference, requestOptions: requestOptions, cancellationToken: ct);
        return intent.Status == "succeeded";
    }

    public async Task<bool> CancelHoldAsync(string externalReference, string idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(externalReference)) return false;

        // CancelAsync voids the hold — Stripe automatically returns funds to the client's card.
        // Only valid while the PaymentIntent is in 'requires_capture' state.
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        PaymentIntent intent = await paymentIntentService.CancelAsync(externalReference, requestOptions: requestOptions, cancellationToken: ct);
        return intent.Status == "canceled";
    }
}
