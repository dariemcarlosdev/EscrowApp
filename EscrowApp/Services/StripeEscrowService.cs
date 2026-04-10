// DEPRECATED — superseded by StripePaymentStrategy in Services/Strategies/.
// Kept to avoid breaking any external references. Will be removed in next cleanup pass.
using Stripe;

namespace EscrowApp.Services;

#pragma warning disable CS0618
[Obsolete("Use StripePaymentStrategy from EscrowApp.Services.Strategies instead.")]
public class StripeEscrowService : IEscrowPaymentService
{
    public async Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId)
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
        var service = new PaymentIntentService();
        PaymentIntent intent = await service.CreateAsync(options);
        return intent.Id;
    }

    public async Task<bool> ReleaseFundsAsync(string stripePaymentIntentId)
    {
        if (string.IsNullOrEmpty(stripePaymentIntentId)) return false;
        var service = new PaymentIntentService();
        PaymentIntent intent = await service.CaptureAsync(stripePaymentIntentId);
        return intent.Status == "succeeded";
    }
}
#pragma warning restore CS0618
