# Clean Architecture Rules

## Layer Map

```
Presentation (Components/) → Application (Features/) → Domain (Models/, Events/, Strategies/)
                                                            ↑
Infrastructure (Data/, Services/, Infrastructure/)──────────┘
```

Inner layers **never** reference outer layers. Dependencies always point inward.

## Layer Boundaries

| Layer | Namespace | Allowed Dependencies |
|-------|-----------|---------------------|
| Domain | `EscrowApp.Models`, `EscrowApp.Events`, `EscrowApp.Services.Strategies` | None — pure C# only |
| Application | `EscrowApp.Features.Escrow.*` | Domain interfaces only |
| Infrastructure | `EscrowApp.Data`, `EscrowApp.Services`, `EscrowApp.Infrastructure` | Domain + Application interfaces |
| Presentation | `EscrowApp.Components` | Application (via IMediator) |

## Forbidden Patterns

- ❌ Domain referencing EF Core, ASP.NET, Stripe SDK, or MediatR
- ❌ Features/ importing `EscrowDbContext` or any concrete infrastructure type
- ❌ Components/ calling repositories or services directly — use `IMediator.Send()` only
- ❌ Circular dependencies between layers

## DI Registration

All services registered in `Program.cs`:
- `AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>()`
- `AddScoped<IEscrowPaymentStrategy, StripeEscrowService>()`
- `AddSingleton<IEventBus, InMemoryEventBus>()`
- `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`

## SOLID Enforcement

- **SRP:** One class, one reason to change — split when responsibilities diverge
- **OCP:** Extend via Strategy pattern (new payment providers) without modifying existing code
- **LSP:** All `IEscrowPaymentStrategy` implementations are substitutable
- **ISP:** `IFundHoldable`, `IFundReleasable`, `IFundCancellable` — not one god interface
- **DIP:** Depend on abstractions (`IRepository`, `IEventBus`), not concrete implementations
