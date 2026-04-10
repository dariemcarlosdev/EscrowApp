using EscrowApp.Models.Repositories;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.ReleaseFunds;

/// <summary>
/// Owns ALL logic for the ReleaseFunds slice. Resolves the correct strategy
/// from ExternalProvider stored on the transaction (supports Stripe today, Ethereum V3).
/// </summary>
internal sealed class ReleaseFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory) : IRequestHandler<ReleaseFundsCommand, ReleaseFundsResult>
{
    public async Task<ReleaseFundsResult> Handle(ReleaseFundsCommand command, CancellationToken ct)
    {
        var transaction = await repo.GetByIdAsync(command.TransactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {command.TransactionId} not found.");

        if (string.IsNullOrEmpty(transaction.ExternalReference) || string.IsNullOrEmpty(transaction.ExternalProvider))
            throw new InvalidOperationException("Transaction is not in a valid state for release.");

        var releaseStrategy = strategyFactory.ResolveReleaseStrategy(transaction.ExternalProvider);

        bool success = await releaseStrategy.ReleaseFundsAsync(
            transaction.ExternalReference,
            idempotencyKey: $"release-{command.TransactionId}",
            ct);

        if (success)
        {
            transaction.Status = "Completed (Released)";
            await repo.UpdateAsync(transaction, ct);
        }

        return new ReleaseFundsResult(transaction.Id, transaction.Status, success);
    }
}
