namespace EscrowApp.Features.Escrow.HoldFunds;

/// <summary>
/// Slice-specific DTO. The UI only ever sees this — never a raw EscrowTransaction entity.
/// </summary>
public sealed record HoldFundsResult(
    int TransactionId,
    string Status,
    string ExternalReference,
    string ExternalProvider,
    decimal Amount);
