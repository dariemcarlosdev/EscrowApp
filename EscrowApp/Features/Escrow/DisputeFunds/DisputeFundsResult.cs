namespace EscrowApp.Features.Escrow.DisputeFunds;

public sealed record DisputeFundsResult(
    int TransactionId,
    string Status,
    bool HoldCancelled,
    string DisputeReason);
