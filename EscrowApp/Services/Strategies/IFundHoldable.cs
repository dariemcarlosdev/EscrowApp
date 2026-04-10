namespace EscrowApp.Services.Strategies;

/// <summary>
/// ISP capability: providers that support Auth-only holds (Stripe manual capture).
/// Providers with immediate settlement (ACH, some crypto) do NOT implement this.
/// </summary>
public interface IFundHoldable
{
    Task<string> HoldFundsAsync(decimal amount, string sourcePaymentMethodId, string idempotencyKey, CancellationToken ct = default);
}
