# 03 — Escrow: Dispute Funds

> Raise a dispute on a held escrow transaction. The payment hold is cancelled
> (auto-refunded), and the dispute is recorded for admin review.

## Status: Implemented

---

## Overview

Either the client or consultant can raise a dispute while funds are in the
**"Funded (Held)"** state. The dispute handler cancels the Stripe PaymentIntent,
which **automatically refunds** the authorized amount to the client's payment method.
A `DisputeRaisedEvent` is published for admin review and future webhook delivery.

## MediatR Command

```csharp
// File: Features/Escrow/DisputeFunds/DisputeFundsCommand.cs
public sealed record DisputeFundsCommand(
    int    TransactionId,
    string Reason,
    string RaisedBy          // "Client" or "Consultant"
) : IRequest<DisputeFundsResult>;
```

## Result DTO

```csharp
// File: Features/Escrow/DisputeFunds/DisputeFundsResult.cs
public sealed record DisputeFundsResult(
    int    TransactionId,
    string Status,
    bool   HoldCancelled,    // Whether the Stripe hold was successfully voided
    string DisputeReason
);
```

## Handler Flow

```
UI ──Send(DisputeFundsCommand)──► DisputeFundsHandler
                                       │
                            1. Retrieve EscrowTransaction by ID
                            2. Validate Status == "Funded (Held)"
                            3. Validate ExternalReference and ExternalProvider exist
                            4. Resolve IFundCancellable strategy (from stored provider)
                            5. Call strategy.CancelHoldAsync(externalRef, idempotencyKey)
                            6. Update transaction:
                               ├── Status        = "Disputed"
                               └── DisputeReason = command.Reason
                            7. Publish DisputeRaisedEvent via IEventBus
                            8. Return DisputeFundsResult
```

## Sequence Diagram

```
Client UI          MediatR           DisputeFundsHandler     StripePaymentStrategy       Stripe API
    │                 │                     │                        │                       │
    │─Send(Command)──►│                     │                        │                       │
    │                 │──Handle(cmd, ct)───►│                        │                       │
    │                 │                     │──GetByIdAsync(id)─────►│(Repository)           │
    │                 │                     │◄─EscrowTransaction─────│                       │
    │                 │                     │                        │                       │
    │                 │                     │  Validate: Status ==   │                       │
    │                 │                     │  "Funded (Held)"       │                       │
    │                 │                     │                        │                       │
    │                 │                     │──CancelHoldAsync()────►│                        │
    │                 │                     │                        │──Cancel PaymentIntent─►│
    │                 │                     │                        │  (auto-refund)         │
    │                 │                     │                        │◄─status: "canceled"────│
    │                 │                     │◄─true──────────────────│                       │
    │                 │                     │                        │                       │
    │                 │                     │──UpdateAsync(tx)──────►│(Repository)           │
    │                 │                     │──PublishAsync(event)──►│(EventBus)             │
    │                 │◄─DisputeFundsResult──│                       │                       │
    │◄─Result─────────│                     │                        │                       │
```

## Status Transition

```
  ┌───────────────┐    DisputeFunds    ┌──────────┐
  │ Funded (Held) │ ──────────────────► │ Disputed │
  └───────────────┘                     └──────────┘
```

> **Note**: Only transactions in "Funded (Held)" status can be disputed.
> Pending or already-released transactions will be rejected.

## Key Classes

| Class                      | File                                                  | Responsibility                          |
| -------------------------- | ----------------------------------------------------- | --------------------------------------- |
| `DisputeFundsCommand`     | `Features/Escrow/DisputeFunds/DisputeFundsCommand.cs`  | Command with reason and raised-by       |
| `DisputeFundsHandler`     | `Features/Escrow/DisputeFunds/DisputeFundsHandler.cs`   | Validates state, cancels hold, records  |
| `DisputeFundsResult`      | `Features/Escrow/DisputeFunds/DisputeFundsResult.cs`    | Result with cancellation status         |
| `DisputeRaisedEvent`      | `Events/DisputeRaisedEvent.cs`                          | Domain event for admin notification     |

## Stripe Details

- **API Call**: `PaymentIntentService.CancelAsync(externalReference)` with idempotency key
- **Auto-Refund**: Cancelling a PaymentIntent in `requires_capture` state automatically
  releases the authorization — funds return to the client's card
- **Success Check**: `PaymentIntent.Status == "canceled"`
- **Idempotency Key**: `$"cancel-{transactionId}"`
- **Timing**: Must cancel before the 7-day Stripe hold expiration

## Domain Event Published

```csharp
new DisputeRaisedEvent
{
    TransactionId    = transaction.Id,
    DisputeReason    = command.Reason,
    RaisedBy         = command.RaisedBy,
    ExternalReference = transaction.ExternalReference
}
```

Currently handled by `InMemoryEventBus` (logs to console). In production this would
trigger admin email notifications, Slack alerts, or webhook delivery.

## Error Handling

- Transaction not found → exception from repository
- Status ≠ "Funded (Held)" → `InvalidOperationException` (cannot dispute non-held tx)
- Missing external reference → `InvalidOperationException`
- Strategy not registered → `InvalidOperationException` from factory
- Stripe cancel fails → exception propagates; status remains "Funded (Held)"
