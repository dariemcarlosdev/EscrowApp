namespace EscrowApp.Events;

/// <summary>
/// Emitted after a payment hold is voluntarily cancelled by mutual agreement.
/// Distinct from DisputeRaisedEvent — cancel is cooperative, dispute is adversarial.
///
/// Published by: CancelFundsHandler after CancelHoldAsync succeeds and status is persisted.
/// Maps to: Stripe void confirmation or future smart-contract release event.
///
/// Regulatory note: Reason and CancelledBy are required for audit traceability —
/// even though no revenue is generated on cancellation, the state change must be fully traceable.
/// </summary>
public sealed class FundsCancelledEvent : DomainEvent
{
    public int TransactionId { get; init; }
    public decimal EscrowAmount { get; init; }
    public string ExternalReference { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string CancelledBy { get; init; } = string.Empty;
}
