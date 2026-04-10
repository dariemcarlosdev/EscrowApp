namespace EscrowApp.Events;

/// <summary>
/// Emitted when a dispute is raised on a held transaction.
/// Maps to a manual admin review flow or a future dispute-resolution Smart Contract in V3.
/// </summary>
public sealed class DisputeRaisedEvent : DomainEvent
{
    public int TransactionId { get; init; }
    public string DisputeReason { get; init; } = string.Empty;
    public string RaisedBy { get; init; } = string.Empty;  // "Client" | "Consultant"
    public string ExternalReference { get; init; } = string.Empty;
}
