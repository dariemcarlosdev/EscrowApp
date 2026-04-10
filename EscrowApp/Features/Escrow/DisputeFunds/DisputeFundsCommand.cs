using MediatR;

namespace EscrowApp.Features.Escrow.DisputeFunds;

/// <summary>
/// Raises a dispute on a held-funds transaction.
/// RaisedBy: "Client" | "Consultant"
/// </summary>
public sealed record DisputeFundsCommand(
    int TransactionId,
    string Reason,
    string RaisedBy) : IRequest<DisputeFundsResult>;
