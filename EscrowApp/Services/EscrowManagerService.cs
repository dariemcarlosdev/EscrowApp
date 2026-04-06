using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Models;
using EscrowApp.Services.Strategies;

namespace EscrowApp.Services;

/// <summary>
/// Application Facade: orchestrates the Repository, Strategy Factory, and Event Bus.
/// The UI never touches infrastructure directly — only this interface.
/// </summary>
public sealed class EscrowManagerService(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IEscrowManagerService
{
    public async Task<EscrowTransaction> ProcessHoldAsync(
        int transactionId, string paymentMethodId, string providerName = "Stripe")
    {
        var transaction = await repo.GetByIdAsync(transactionId)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        var holdStrategy = strategyFactory.ResolveHoldStrategy(providerName);
        string externalReference = await holdStrategy.HoldFundsAsync(
            transaction.Amount, paymentMethodId, idempotencyKey: $"hold-{transactionId}");

        transaction.ExternalReference = externalReference;
        transaction.ExternalProvider = providerName;
        transaction.Status = "Funded (Held)";
        await repo.UpdateAsync(transaction);

        await eventBus.PublishAsync(new PaymentReceivedEvent
        {
            TransactionId = transactionId,
            Amount = transaction.Amount,
            ExternalReference = externalReference,
            Provider = providerName
        });

        return transaction;
    }

    public async Task<EscrowTransaction> ProcessReleaseAsync(int transactionId)
    {
        var transaction = await repo.GetByIdAsync(transactionId)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        if (string.IsNullOrEmpty(transaction.ExternalReference) || string.IsNullOrEmpty(transaction.ExternalProvider))
            throw new InvalidOperationException("Transaction is not in a valid state for release.");

        var releaseStrategy = strategyFactory.ResolveReleaseStrategy(transaction.ExternalProvider);
        bool success = await releaseStrategy.ReleaseFundsAsync(
            transaction.ExternalReference, idempotencyKey: $"release-{transactionId}");

        if (success)
        {
            transaction.Status = "Completed (Released)";
            await repo.UpdateAsync(transaction);
        }

        return transaction;
    }
}
