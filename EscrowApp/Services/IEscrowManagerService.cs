using EscrowApp.Models;

namespace EscrowApp.Services;

public interface IEscrowManagerService
{
    Task<EscrowTransaction> ProcessHoldAsync(int transactionId, string paymentMethodId, string providerName = "Stripe");
    Task<EscrowTransaction> ProcessReleaseAsync(int transactionId);
}
