// DEPRECATED — superseded by IFundHoldable and IFundReleasable in Services/Strategies/.
// Kept to avoid breaking any external references. Will be removed in next cleanup pass.
namespace EscrowApp.Services;

[Obsolete("Use IFundHoldable and IFundReleasable from EscrowApp.Services.Strategies instead.")]
public interface IEscrowPaymentService
{
    Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId);
    Task<bool> ReleaseFundsAsync(string stripePaymentIntentId);
}
