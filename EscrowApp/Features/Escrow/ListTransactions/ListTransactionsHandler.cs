using EscrowApp.Features.Escrow.Api;
using EscrowApp.Models.Repositories;
using MediatR;

namespace EscrowApp.Features.Escrow.ListTransactions;

internal sealed class ListTransactionsHandler(
    IEscrowTransactionRepository repo)
    : IRequestHandler<ListTransactionsQuery, PaginatedResponse<EscrowTransactionResponse>>
{
    public async Task<PaginatedResponse<EscrowTransactionResponse>> Handle(
        ListTransactionsQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        var (items, totalCount) = await repo.ListAsync(query.Status, page, pageSize, ct);

        return new PaginatedResponse<EscrowTransactionResponse>
        {
            Items = items.Select(t => new EscrowTransactionResponse
            {
                Id = t.Id,
                ClientEmail = t.ClientEmail,
                ConsultantEmail = t.ConsultantEmail,
                Amount = t.Amount,
                ServiceDescription = t.ServiceDescription,
                Status = t.Status,
                ExternalReference = t.ExternalReference,
                ExternalProvider = t.ExternalProvider,
                DisputeReason = t.DisputeReason,
                CreatedAt = t.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
