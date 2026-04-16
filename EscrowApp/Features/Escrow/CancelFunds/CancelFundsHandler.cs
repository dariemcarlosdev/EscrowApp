using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.CancelFunds;

/// <summary>
/// Implements the CancelFunds vertical slice.
///
/// Cancels a held payment — voids the Stripe PaymentIntent authorization
/// and returns funds to the client's card. This is a cooperative action
/// agreed upon by both parties, distinct from DisputeFunds (adversarial).
///
/// Architecture decisions applied (see docs/architecture/payment-strategies.md):
/// - Strategy resolved via IPaymentStrategyFactory.ResolveCancelStrategy()
/// - IFundCancellable.CancelHoldAsync() called with caller-supplied idempotency key
///
/// Architecture decisions applied (see docs/architecture/event-bus.md):
/// - FundsCancelledEvent published AFTER persistence (events reflect committed state)
///
/// Fintech guardrail: No platform fee is collected on cancelled transactions.
/// The entire Stripe authorization (escrow + fee) is voided.
/// </summary>
internal sealed class CancelFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IRequestHandler<CancelFundsCommand, CancelFundsResult>
{
    private const string HeldStatus = "Funded (Held)";

    public async Task<CancelFundsResult> Handle(CancelFundsCommand command, CancellationToken ct)
    {
        // 1. Load transaction — 404 if not found
        var transaction = await repo.GetByIdAsync(command.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {command.TransactionId} not found.");

        // 2. State guard — only held transactions can be cancelled
        if (transaction.Status != HeldStatus)
            throw new InvalidOperationException(
                $"Cannot cancel transaction in status '{transaction.Status}'. " +
                $"Only '{HeldStatus}' transactions can be cancelled.");

        // 3. Disputed guard — disputed transactions follow a separate resolution path
        if (transaction.Status == "Disputed")
            throw new InvalidOperationException(
                $"Transaction {command.TransactionId} is disputed and cannot be cancelled. " +
                "Disputed transactions require explicit resolution.");

        // 4. External reference guard — must have a Stripe PaymentIntent ID to void
        if (string.IsNullOrEmpty(transaction.ExternalReference) || string.IsNullOrEmpty(transaction.ExternalProvider))
            throw new InvalidOperationException(
                $"Transaction {command.TransactionId} has no external payment reference to cancel.");

        // 5. Resolve IFundCancellable via factory (see docs/architecture/payment-strategies.md)
        var cancelStrategy = strategyFactory.ResolveCancelStrategy(transaction.ExternalProvider);

        // 6. Void the Stripe authorization — caller-supplied idempotency key ensures retry safety
        bool holdCancelled = await cancelStrategy.CancelHoldAsync(
            transaction.ExternalReference,
            idempotencyKey: command.IdempotencyKey,
            ct);

        // 7. Persist first — domain events must reflect committed state (see docs/architecture/event-bus.md)
        transaction.Status = "Cancelled";
        await repo.UpdateAsync(transaction, ct);

        // 8. Publish domain event after successful persistence
        await eventBus.PublishAsync(new FundsCancelledEvent
        {
            TransactionId     = transaction.Id,
            EscrowAmount      = transaction.Amount,
            ExternalReference = transaction.ExternalReference,
            Provider          = transaction.ExternalProvider,
            Reason            = command.Reason,
            CancelledBy       = command.CancelledBy
        }, ct);

        // 9. Return result
        return new CancelFundsResult(
            transaction.Id,
            transaction.Status,
            transaction.ExternalReference,
            transaction.ExternalProvider,
            command.Reason,
            command.CancelledBy);
    }
}
