# Domain-Driven Design Rules

## Aggregate Root

`EscrowTransaction` is the **sole aggregate root** for the escrow lifecycle.

- All state mutations flow through its public behavior methods
- Never expose public setters — use factory methods or behavior methods
- Guard every state transition with precondition checks
- Child entities (e.g., `Actor`) are accessed only through the aggregate

## State Machine

```
Pending → Held → Released
                → Disputed → Resolved → Released | Refunded
         → Cancelled
```

Transitions are enforced inside the entity — throw `InvalidOperationException` on violations.

## Domain Events

- Raised **from within the aggregate** using `AddDomainEvent()` helper
- Events are **past-tense facts**: `PaymentReceivedEvent`, `DisputeRaisedEvent`, `FundsReleasedEvent`
- Events carry only IDs and relevant state — never full entity graphs
- Dispatch events **after persistence** — never before `SaveChangesAsync`

## Value Objects

Use `record` types for concepts with no identity:
- `Money` (amount + currency) — reject negative amounts in constructor
- `IdempotencyKey` — reject empty strings
- `WalletAddress` — validate format

## Strategy Interfaces (Domain Layer)

```csharp
IEscrowPaymentStrategy          // Marker
├── IFundHoldable               // HoldFundsAsync(amount, paymentMethodId, idempotencyKey)
├── IFundReleasable             // ReleaseFundsAsync(externalReference, idempotencyKey)
└── IFundCancellable            // CancelHoldAsync(externalReference, idempotencyKey)
```

Interfaces define **what** the domain needs. Infrastructure provides **how**.

## Pure Domain — No Framework Dependencies

- Domain classes are plain C# POCOs — no `[Table]`, `[Column]`, EF Core attributes
- No references to MediatR, ASP.NET Core, Entity Framework, or Stripe SDK
- Mapping to persistence via Fluent API (`IEntityTypeConfiguration<T>`) in Infrastructure
- Use `DateTimeOffset` for all timestamps, `Guid` for entity IDs
- Collections exposed as `IReadOnlyCollection<T>` — mutation only through aggregate methods

## Entity Model

```
EscrowTransaction
├── Id (int, PK)           ├── ExternalReference (string?)
├── ClientEmail (string)   ├── ExternalProvider (string?)
├── ConsultantEmail        ├── DisputeReason (string?)
├── Amount (decimal)       └── CreatedAt (DateTime, UTC)
├── ServiceDescription
├── Status (string)
```
