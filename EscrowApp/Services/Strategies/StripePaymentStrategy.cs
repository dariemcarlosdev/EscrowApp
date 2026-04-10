using Stripe;

namespace EscrowApp.Services.Strategies;

/// <summary>
/// Stripe strategy: implements hold, release, AND cancel capabilities.
/// Knows NOTHING about EscrowDbContext — pure Stripe SDK orchestration only.
/// Idempotency keys prevent double-actions on network retries (§4).
/// </summary>
public sealed class StripePaymentStrategy : IEscrowPaymentStrategy, IFundHoldable, IFundReleasable, IFundCancellable
{
    public string ProviderName => "Stripe";

    public async Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId, string idempotencyKey)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = "usd",
            PaymentMethod = sourcePaymentMethodId,
            CaptureMethod = "manual",
            Confirm = true,
            ReturnUrl = "http://localhost:5222/payment-return"
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var service = new PaymentIntentService();
        PaymentIntent intent = await service.CreateAsync(options, requestOptions);
        return intent.Id;
    }

    public async Task<bool> ReleaseFundsAsync(string externalReference, string idempotencyKey)
    {
        if (string.IsNullOrEmpty(externalReference)) return false;

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var service = new PaymentIntentService();
        PaymentIntent intent = await service.CaptureAsync(externalReference, requestOptions: requestOptions);
        return intent.Status == "succeeded";
    }

    public async Task<bool> CancelHoldAsync(string externalReference, string idempotencyKey)
    {
        if (string.IsNullOrEmpty(externalReference)) return false;

        // CancelAsync voids the hold — Stripe automatically returns funds to the client's card.
        // Only valid while the PaymentIntent is in 'requires_capture' state.
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var service = new PaymentIntentService();
        PaymentIntent intent = await service.CancelAsync(externalReference, requestOptions: requestOptions);
        return intent.Status == "canceled";
    }
}
