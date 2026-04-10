namespace EscrowApp.Events;

/// <summary>
/// Base for all domain events. Enables the UnifiedEventBus pillar — Stripe webhooks
/// and future Blockchain Indexer events both translate into DomainEvent subclasses
/// before touching business logic.
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
