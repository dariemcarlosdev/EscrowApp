namespace EscrowApp.Features.Escrow.ReleaseFunds;

/// <summary>
/// Slice-specific DTO for the ReleaseFunds slice.
/// </summary>
public sealed record ReleaseFundsResult(
    int TransactionId,
    string Status,
    bool Success);
