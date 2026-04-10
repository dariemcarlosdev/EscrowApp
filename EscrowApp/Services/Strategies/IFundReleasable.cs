namespace EscrowApp.Services.Strategies;

/// <summary>
/// ISP capability: providers that support deferred capture/release.
/// Providers with immediate settlement do NOT implement this.
/// </summary>
public interface IFundReleasable
{
    Task<bool> ReleaseFundsAsync(string externalReference, string idempotencyKey, CancellationToken ct = default);
}
