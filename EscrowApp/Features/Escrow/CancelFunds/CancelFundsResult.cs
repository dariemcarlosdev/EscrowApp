namespace EscrowApp.Features.Escrow.CancelFunds;

/// <summary>
/// Result DTO for the CancelFunds operation.
/// </summary>
public sealed record CancelFundsResult(
    int TransactionId,
    string Status,
    string ExternalReference,
    string ExternalProvider,
    string Reason,
    string CancelledBy);
