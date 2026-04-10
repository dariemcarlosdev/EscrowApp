namespace EscrowApp.Services.Strategies;

/// <summary>
/// ISP capability: providers that support voiding/cancelling an existing hold.
/// Stripe manual-capture PaymentIntents support this — calling CancelAsync
/// lifts the hold and automatically returns funds to the client's card.
/// </summary>
public interface IFundCancellable
{
    Task<bool> CancelHoldAsync(string externalReference, string idempotencyKey, CancellationToken ct = default);
}
