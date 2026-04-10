namespace EscrowApp.Services.Strategies;

/// <summary>
/// Strategy marker interface. Concrete providers implement this plus the
/// capability interfaces (IFundHoldable, IFundReleasable) they actually support.
/// OCP: adding PayPal or Ethereum only requires a new class — zero existing changes.
/// </summary>
public interface IEscrowPaymentStrategy
{
    string ProviderName { get; }
}
