using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.HoldFunds;

/// <summary>
/// Owns ALL logic for the HoldFunds slice — repository access, strategy resolution,
/// idempotency key generation, and event publishing. Nothing leaks out.
/// </summary>
internal sealed class HoldFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IRequestHandler<HoldFundsCommand, HoldFundsResult>
{
    public async Task<HoldFundsResult> Handle(HoldFundsCommand command, CancellationToken ct)
    {
        var transaction = await repo.GetByIdAsync(command.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {command.TransactionId} not found.");

        var holdStrategy = strategyFactory.ResolveHoldStrategy(command.ProviderName);

        string externalReference = await holdStrategy.HoldFundsAsync(
            transaction.Amount,
            command.PaymentMethodId,
            idempotencyKey: $"hold-{command.TransactionId}",
            ct);

        transaction.ExternalReference = externalReference;
        transaction.ExternalProvider = command.ProviderName;
        transaction.Status = "Funded (Held)";
        await repo.UpdateAsync(transaction, ct);

        await eventBus.PublishAsync(new PaymentReceivedEvent
        {
            TransactionId = command.TransactionId,
            Amount = transaction.Amount,
            ExternalReference = externalReference,
            Provider = command.ProviderName
        }, ct);

        return new HoldFundsResult(
            transaction.Id,
            transaction.Status,
            externalReference,
            command.ProviderName,
            transaction.Amount);
    }
}
