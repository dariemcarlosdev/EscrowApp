namespace EscrowApp.Events;

/// <summary>
/// Emitted after funds are successfully held. Maps to Stripe's
/// "payment_intent.amount_capturable_updated" webhook — or a Blockchain
/// Transfer event in V3. Business logic reacts to THIS, never to raw provider callbacks.
/// </summary>
public sealed class PaymentReceivedEvent : DomainEvent
{
    public int TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string ExternalReference { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;

    // Platform fee audit trail — immutable snapshot of fee amount and rate applied at hold time.
    // Required for regulatory traceability per fintech guardrails (AGENTS.md).
    public decimal PlatformFee { get; init; }
    public decimal PlatformFeePercentage { get; init; }
}
