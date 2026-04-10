using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.CancelFunds;

/// <summary>
/// Cancels a held payment — voids the Stripe PaymentIntent authorization
/// and returns funds to the client's card. This is a voluntary action,
/// distinct from DisputeFunds which is a contested action.
/// </summary>
internal sealed class CancelFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IRequestHandler<CancelFundsCommand, CancelFundsResult>
{
    public async Task<CancelFundsResult> Handle(CancelFundsCommand command, CancellationToken ct)
    {
        // TODO: Implement cancel funds flow
        // 1. Load transaction by ID — throw if not found
        // 2. Validate status is "Funded (Held)" — only held transactions can be cancelled
        // 3. Resolve IFundCancellable strategy via IPaymentStrategyFactory
        // 4. Call CancelHoldAsync(externalReference, idempotencyKey)
        // 5. Update transaction status to "Cancelled"
        // 6. Persist via repository.UpdateAsync()
        // 7. Publish domain event (FundsCancelledEvent) via IEventBus
        // 8. Return CancelFundsResult

        throw new NotImplementedException(
            "CancelFunds handler not yet implemented. See TODO comments for implementation steps.");
    }
}
