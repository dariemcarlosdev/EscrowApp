namespace EscrowApp.Events;

/// <summary>
/// Abstraction for the UnifiedEventBus (§0.2). Swap InMemoryEventBus for
/// MassTransit, Azure Service Bus, or a Blockchain Indexer feed without
/// touching any business logic.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : DomainEvent;
}
