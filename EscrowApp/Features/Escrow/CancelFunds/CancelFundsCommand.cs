using MediatR;

namespace EscrowApp.Features.Escrow.CancelFunds;

/// <summary>
/// Cancels (voids) a held-funds transaction. Unlike DisputeFunds, this represents
/// a voluntary cancellation agreed upon by both parties — no dispute is raised.
///
/// State transition: "Funded (Held)" → "Cancelled"
/// Dispute transition: "Funded (Held)" → "Disputed" (use DisputeFundsCommand instead)
/// </summary>
public sealed record CancelFundsCommand(
    int TransactionId,
    string Reason,
    string CancelledBy,
    string IdempotencyKey) : IRequest<CancelFundsResult>;
