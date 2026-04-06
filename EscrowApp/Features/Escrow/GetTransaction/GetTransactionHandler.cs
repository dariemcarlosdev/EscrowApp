using EscrowApp.Models.Repositories;
using EscrowApp.Features.Escrow.Api;
using EscrowApp.Models;
using MediatR;

namespace EscrowApp.Features.Escrow.GetTransaction;

internal sealed class GetTransactionHandler(
    IEscrowTransactionRepository repo)
    : IRequestHandler<GetTransactionQuery, EscrowTransactionResponse?>
{
    public async Task<EscrowTransactionResponse?> Handle(
        GetTransactionQuery query, CancellationToken ct)
    {
        var tx = await repo.GetByIdAsync(query.TransactionId);
        return tx is null ? null : MapToResponse(tx);
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
