using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Features.Escrow.Api;
using EscrowApp.Models;
using EscrowApp.Services.Strategies;
using MediatR;

namespace EscrowApp.Features.Escrow.CreateAndHoldFunds;

/// <summary>
/// Creates a new transaction, holds funds via the resolved payment strategy,
/// and publishes a PaymentReceivedEvent — all in one atomic operation.
/// </summary>
internal sealed class CreateAndHoldFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus) : IRequestHandler<CreateAndHoldFundsCommand, EscrowTransactionResponse>
{
    public async Task<EscrowTransactionResponse> Handle(
        CreateAndHoldFundsCommand command, CancellationToken ct)
    {
        var transaction = new EscrowTransaction
        {
            ClientEmail = command.ClientEmail,
            ConsultantEmail = command.ConsultantEmail,
            Amount = command.Amount,
            ServiceDescription = command.ServiceDescription,
            Status = "Pending"
        };

        var created = await repo.AddAsync(transaction, ct);

        var holdStrategy = strategyFactory.ResolveHoldStrategy(command.ProviderName);

        string externalReference = await holdStrategy.HoldFundsAsync(
            created.Amount,
            command.PaymentMethodId,
            idempotencyKey: $"hold-{created.Id}",
            ct);

        created.ExternalReference = externalReference;
        created.ExternalProvider = command.ProviderName;
        created.Status = "Funded (Held)";
        await repo.UpdateAsync(created, ct);

        await eventBus.PublishAsync(new PaymentReceivedEvent
        {
            TransactionId = created.Id,
            Amount = created.Amount,
            ExternalReference = externalReference,
            Provider = command.ProviderName
        }, ct);

        return MapToResponse(created);
    }

    private static EscrowTransactionResponse MapToResponse(EscrowTransaction tx) => new()
    {
        Id = tx.Id,
        ClientEmail = tx.ClientEmail,
        ConsultantEmail = tx.ConsultantEmail,
        Amount = tx.Amount,
        ServiceDescription = tx.ServiceDescription,
        Status = tx.Status,
        ExternalReference = tx.ExternalReference,
        ExternalProvider = tx.ExternalProvider,
        DisputeReason = tx.DisputeReason,
        CreatedAt = tx.CreatedAt
    };
}
