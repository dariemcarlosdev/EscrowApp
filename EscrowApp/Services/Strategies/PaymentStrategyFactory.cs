namespace EscrowApp.Services.Strategies;

/// <summary>
/// Resolves the correct payment strategy at runtime by ProviderName.
/// OCP: adding a new provider only requires registering a new IEscrowPaymentStrategy
/// in Program.cs — zero changes to this factory or any handler.
/// </summary>
public sealed class PaymentStrategyFactory(IEnumerable<IEscrowPaymentStrategy> strategies) : IPaymentStrategyFactory
{
    public IFundHoldable ResolveHoldStrategy(string providerName)
        => Resolve<IFundHoldable>(providerName, "fund holds");

    public IFundReleasable ResolveReleaseStrategy(string providerName)
        => Resolve<IFundReleasable>(providerName, "fund releases");

    public IFundCancellable ResolveCancelStrategy(string providerName)
        => Resolve<IFundCancellable>(providerName, "hold cancellations");

    private T Resolve<T>(string providerName, string capability) where T : class
    {
        var strategy = strategies.FirstOrDefault(s => s.ProviderName == providerName)
            ?? throw new InvalidOperationException($"No strategy registered for provider '{providerName}'.");

        return strategy as T
            ?? throw new NotSupportedException($"Provider '{providerName}' does not support {capability}.");
    }
}
