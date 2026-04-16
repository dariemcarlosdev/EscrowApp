using MediatR;

namespace EscrowApp.Features.Escrow.DisputeFunds;

/// <summary>
/// Raises a dispute on a held-funds transaction.
/// RaisedBy: "Client" | "Consultant"
/// 
/// IdempotencyKey is required for Stripe PaymentIntent void operation idempotency.
/// </summary>
public sealed record DisputeFundsCommand(
    int TransactionId,
    string Reason,
    string RaisedBy,
    string IdempotencyKey) : IRequest<DisputeFundsResult>;
