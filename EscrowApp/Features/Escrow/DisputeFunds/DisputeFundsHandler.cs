using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.DisputeFunds;

/// <summary>
/// Dispute slice handler:
/// 1. Validates the transaction is in a held state (can only dispute an active hold).
/// 2. Cancels the Stripe hold via IFundCancellable — Stripe automatically returns
///    funds to the client's card; no new charge is issued.
/// 3. Marks transaction as "Disputed" with reason and raises DisputeRaisedEvent
///    for async admin review (or future Smart Contract arbitration in V3).
/// </summary>
internal sealed class DisputeFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IRequestHandler<DisputeFundsCommand, DisputeFundsResult>
{
    private const string HeldStatus = "Funded (Held)";

    public async Task<DisputeFundsResult> Handle(DisputeFundsCommand command, CancellationToken ct)
    {
        var transaction = await repo.GetByIdAsync(command.TransactionId)
            ?? throw new InvalidOperationException($"Transaction {command.TransactionId} not found.");

        if (transaction.Status != HeldStatus)
            throw new InvalidOperationException(
                $"Cannot dispute transaction in status '{transaction.Status}'. Only '{HeldStatus}' transactions can be disputed.");

        if (string.IsNullOrEmpty(transaction.ExternalReference) || string.IsNullOrEmpty(transaction.ExternalProvider))
            throw new InvalidOperationException("Transaction has no external payment reference to cancel.");

        var cancelStrategy = strategyFactory.ResolveCancelStrategy(transaction.ExternalProvider);

        bool holdCancelled = await cancelStrategy.CancelHoldAsync(
            transaction.ExternalReference,
            idempotencyKey: $"dispute-{command.TransactionId}");

        transaction.Status = "Disputed";
        transaction.DisputeReason = command.Reason;
        await repo.UpdateAsync(transaction);

        await eventBus.PublishAsync(new DisputeRaisedEvent
        {
            TransactionId = command.TransactionId,
            DisputeReason = command.Reason,
            RaisedBy = command.RaisedBy,
            ExternalReference = transaction.ExternalReference
        }, ct);

        return new DisputeFundsResult(
            transaction.Id,
            transaction.Status,
            holdCancelled,
            command.Reason);
    }
}
