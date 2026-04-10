using EscrowApp.Features.Escrow.Api;
using MediatR;

namespace EscrowApp.Features.Escrow.ListTransactions;

/// <summary>
/// Paginated query to list escrow transactions.
/// </summary>
public sealed record ListTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null) : IRequest<PaginatedResponse<EscrowTransactionResponse>>;
