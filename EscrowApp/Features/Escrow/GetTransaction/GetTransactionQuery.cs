using EscrowApp.Features.Escrow.Api;
using MediatR;

namespace EscrowApp.Features.Escrow.GetTransaction;

/// <summary>
/// Query to retrieve a single escrow transaction by ID.
/// </summary>
public sealed record GetTransactionQuery(int TransactionId) : IRequest<EscrowTransactionResponse?>;
