using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Features.Escrow.Api;
using EscrowApp.Shared.Configuration;
using EscrowApp.Models;
using EscrowApp.Services.Strategies;
using MediatR;
using Microsoft.Extensions.Options;

namespace EscrowApp.Features.Escrow.CreateAndHoldFunds;

/// <summary>
/// Creates a new transaction, calculates the platform fee, holds the total (escrow + fee)
/// via the resolved payment strategy, and publishes a PaymentReceivedEvent — one atomic operation.
///
/// Fee rule: platformFee = max(amount × feePercentage, minimumFee)
/// Stripe is charged: escrowAmount + platformFee  (client pays both)
/// Consultant receives on release: escrowAmount − Stripe processing fee
/// NexTruzt.io retains: platformFee (stays in platform Stripe balance)
/// </summary>
internal sealed class CreateAndHoldFundsHandler(
    IEscrowTransactionRepository repo,
    IPaymentStrategyFactory strategyFactory,
    IEventBus eventBus,
    IOptions<PlatformOptions> platformOptions) : IRequestHandler<CreateAndHoldFundsCommand, EscrowTransactionResponse>
{
    public async Task<EscrowTransactionResponse> Handle(
        CreateAndHoldFundsCommand command, CancellationToken ct)
    {
        // --- Fee calculation (fintech guardrail: never modify amounts outside this layer) ---
        var options = platformOptions.Value;
        var platformFee = Math.Max(
            command.Amount * options.FeePercentage,
            options.MinimumFee);

        var transaction = new EscrowTransaction
        {
            ClientEmail           = command.ClientEmail,
            ConsultantEmail       = command.ConsultantEmail,
            Amount                = command.Amount,
            ServiceDescription    = command.ServiceDescription,
            Status                = "Pending",
            // Snapshot fee at creation — immutable for audit trail integrity
            PlatformFee           = platformFee,
            PlatformFeePercentage = options.FeePercentage
        };

        var created = await repo.AddAsync(transaction, ct);

        var holdStrategy = strategyFactory.ResolveHoldStrategy(command.ProviderName);

        // Stripe is charged the full amount: escrow amount PLUS platform fee
        var totalCharge = created.Amount + created.PlatformFee;

        string externalReference = await holdStrategy.HoldFundsAsync(
            totalCharge,
            command.PaymentMethodId,
            idempotencyKey: $"hold-{created.Id}",
            ct);

        created.ExternalReference = externalReference;
        created.ExternalProvider  = command.ProviderName;
        created.Status            = "Funded (Held)";
        await repo.UpdateAsync(created, ct);

        await eventBus.PublishAsync(new PaymentReceivedEvent
        {
            TransactionId         = created.Id,
            Amount                = created.Amount,
            ExternalReference     = externalReference,
            Provider              = command.ProviderName,
            PlatformFee           = created.PlatformFee,
            PlatformFeePercentage = created.PlatformFeePercentage
        }, ct);

        return MapToResponse(created);
    }

    private static EscrowTransactionResponse MapToResponse(EscrowTransaction tx) => new()
    {
        Id                    = tx.Id,
        ClientEmail           = tx.ClientEmail,
        ConsultantEmail       = tx.ConsultantEmail,
        Amount                = tx.Amount,
        ServiceDescription    = tx.ServiceDescription,
        Status                = tx.Status,
        ExternalReference     = tx.ExternalReference,
        ExternalProvider      = tx.ExternalProvider,
        DisputeReason         = tx.DisputeReason,
        PlatformFee           = tx.PlatformFee,
        PlatformFeePercentage = tx.PlatformFeePercentage,
        CreatedAt             = tx.CreatedAt
    };
}
