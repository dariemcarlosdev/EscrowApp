namespace EscrowApp.Services.Strategies;

public interface IPaymentStrategyFactory
{
    IFundHoldable ResolveHoldStrategy(string providerName);
    IFundReleasable ResolveReleaseStrategy(string providerName);
    IFundCancellable ResolveCancelStrategy(string providerName);
}
