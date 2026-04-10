# 06 — Event Bus

> Decoupled domain event publishing for side effects — notifications, auditing,
> webhook delivery, and future message broker integration.

## Status: Implemented (InMemory MVP); MassTransit/RabbitMQ planned

---

## Overview

The Event Bus decouples core escrow operations from their side effects. When funds are
held or a dispute is raised, the handler publishes a **domain event** via `IEventBus`.
The current MVP implementation logs events to the console. In production, this abstraction
is swapped for MassTransit, Azure Service Bus, or a blockchain event indexer — with
**zero changes** to the handlers.

## Domain Event Base Class

```csharp
// File: Events/DomainEvent.cs
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

Every domain event carries:
- **EventId**: Unique identifier for idempotent processing and deduplication
- **OccurredAt**: UTC timestamp for ordering and audit trails

## Event Bus Interface

```csharp
// File: Events/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : DomainEvent;
}
```

## Current Implementation: InMemoryEventBus

```csharp
// File: Events/InMemoryEventBus.cs
public sealed class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : DomainEvent
    {
        logger.LogInformation(
            "[EventBus] Published {EventType} | EventId={EventId} | At={OccurredAt}",
            typeof(T).Name, domainEvent.EventId, domainEvent.OccurredAt);
        return Task.CompletedTask;
    }
}
```

**DI Registration:**

```csharp
// Program.cs
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();
```

## Domain Events

### PaymentReceivedEvent

```csharp
// File: Events/PaymentReceivedEvent.cs
public sealed class PaymentReceivedEvent : DomainEvent
{
    public int TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string ExternalReference { get; init; } = "";   // Stripe PI ID or tx hash
    public string Provider { get; init; } = "";            // "Stripe", "Ethereum"
}
```

**Published by:** `HoldFundsHandler` after successful fund authorization.

**Maps to:** Stripe's `payment_intent.amount_capturable_updated` webhook or
blockchain `Transfer` event.

### DisputeRaisedEvent

```csharp
// File: Events/DisputeRaisedEvent.cs
public sealed class DisputeRaisedEvent : DomainEvent
{
    public int TransactionId { get; init; }
    public string DisputeReason { get; init; } = "";
    public string RaisedBy { get; init; } = "";            // "Client" or "Consultant"
    public string ExternalReference { get; init; } = "";
}
```

**Published by:** `DisputeFundsHandler` after cancelling the payment hold.

**Maps to:** Manual admin review queue or future dispute-resolution smart contract.

## Event Flow Diagram

```
HoldFundsHandler ──PublishAsync──► IEventBus ──► InMemoryEventBus ──► Console Log
                                       │
DisputeFundsHandler ──PublishAsync──────┘
                                       │
                                  (Future)
                                       │
                          ┌────────────┼────────────┐
                          ▼            ▼            ▼
                     MassTransit   Azure SB    Webhook HTTP
                     /RabbitMQ                  Delivery
```

## Where Events Are Published

| Handler                | Event                  | Trigger                          |
| ---------------------- | ---------------------- | -------------------------------- |
| `HoldFundsHandler`     | `PaymentReceivedEvent` | After funds successfully held    |
| `DisputeFundsHandler`  | `DisputeRaisedEvent`   | After hold cancelled + disputed  |

> **Note:** `ReleaseFundsHandler` does not currently publish an event.
> A `FundsReleasedEvent` is planned for consultant notifications.

## Future: Production Event Bus

### MassTransit + RabbitMQ

```csharp
// Future: Replace InMemoryEventBus registration
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddScoped<IEventBus, MassTransitEventBus>();
```

### Planned Events

| Event                  | Status      | Use Case                               |
| ---------------------- | ----------- | -------------------------------------- |
| `PaymentReceivedEvent` | Implemented | Notify consultant, update dashboard    |
| `DisputeRaisedEvent`   | Implemented | Admin alert, audit log                 |
| `FundsReleasedEvent`   | Planned     | Consultant payment notification        |
| `EscrowCreatedEvent`   | Planned     | Welcome email, onboarding flow         |
| `EscrowExpiredEvent`   | Planned     | Auto-refund after hold expiration      |

## Source Files

| File                               | Purpose                               |
| ---------------------------------- | ------------------------------------- |
| `Events/DomainEvent.cs`           | Abstract base with EventId + timestamp |
| `Events/IEventBus.cs`             | Publishing abstraction                 |
| `Events/InMemoryEventBus.cs`      | MVP implementation (console logging)   |
| `Events/PaymentReceivedEvent.cs`  | Emitted when funds are held            |
| `Events/DisputeRaisedEvent.cs`    | Emitted when dispute is raised         |
