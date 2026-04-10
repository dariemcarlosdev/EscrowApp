using Microsoft.Extensions.Logging;

namespace EscrowApp.Events;

/// <summary>
/// MVP stub. Logs events to the console.
/// Replace with MassTransit or Azure Service Bus in production.
/// </summary>
public sealed class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : DomainEvent
    {
        logger.LogInformation(
            "[EventBus] {EventType} | Id={EventId} | At={OccurredAt}",
            typeof(T).Name, domainEvent.EventId, domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
