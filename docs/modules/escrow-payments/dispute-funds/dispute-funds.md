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

## User Stories

Stories for the adversarial dispute flow. **User-facing copy must use *dispute held funds* — never *escrow*.** A dispute voids the Stripe authorization (auto-refund to the client) and records the dispute for admin review.

### Story 1 — Client raises a dispute

**As a** Client, **I want** to raise a dispute on a held payment when the consultant has not delivered the agreed service, **so that** my authorized funds are not captured and an admin can review the case.

**Acceptance Criteria:**

- [ ] IFundCancellable.CancelHoldAsync is called for "pi_abc123"
- [ ] the Stripe hold is voided (auto-refund to the client's payment method)
- [ ] the transaction status transitions to "Disputed"
- [ ] DisputeReason is persisted
- [ ] a DisputeRaisedEvent is published after persistence

```gherkin
Feature: Client-initiated dispute
  Scenario: Dispute raised on Funded (Held) transaction
    Given a transaction in status "Funded (Held)" with ExternalReference "pi_abc123"
    When the client submits DisputeFundsCommand with RaisedBy="Client" and a reason
    Then IFundCancellable.CancelHoldAsync is called for "pi_abc123"
    And the Stripe hold is voided (auto-refund to the client's payment method)
    And the transaction status transitions to "Disputed"
    And DisputeReason is persisted
    And a DisputeRaisedEvent is published after persistence
```

### Story 2 — Consultant raises a dispute

**As a** Consultant, **I want** to raise a dispute when the client refuses to release funds despite delivery, **so that** the platform admin reviews the engagement instead of the funds being silently cancelled by the client.

**Acceptance Criteria:**

- [ ] the transaction status transitions to "Disputed"
- [ ] DisputeRaisedEvent.RaisedBy = "Consultant"

```gherkin
Feature: Consultant-initiated dispute
  Scenario: Dispute raised by consultant
    Given a transaction in status "Funded (Held)"
    When the consultant submits DisputeFundsCommand with RaisedBy="Consultant"
    Then the transaction status transitions to "Disputed"
    And DisputeRaisedEvent.RaisedBy = "Consultant"
```

### Story 3 — Dispute prevents subsequent release

**As a** Compliance Officer, **I want** a disputed transaction to be ineligible for release, **so that** funds cannot be captured while a dispute is open and the regulatory audit trail remains consistent.

**Acceptance Criteria:**

- [ ] the handler rejects with InvalidOperationException
- [ ] the transaction status remains "Disputed"
- [ ] no Stripe capture call is made

```gherkin
Feature: Dispute blocks release
  Scenario: Release attempted after dispute
    Given a transaction in status "Disputed"
    When ReleaseFundsCommand is submitted for that transaction
    Then the handler rejects with InvalidOperationException
    And the transaction status remains "Disputed"
    And no Stripe capture call is made
```

### Story 4 — Admin reviews open disputes

**As a** Platform Admin, **I want** every dispute to emit a `DisputeRaisedEvent` carrying the reason, parties, and external reference, **so that** I can build review queues and webhook deliveries without polling the database.

**Acceptance Criteria:**

- [ ] the DisputeRaisedEvent contains TransactionId, RaisedBy, Reason, and ExternalReference
- [ ] the event is published only after the transaction row is committed

```gherkin
Feature: Dispute event for downstream review
  Scenario: Event payload is complete
    When a dispute is raised on transaction 42
    Then the DisputeRaisedEvent contains TransactionId, RaisedBy, Reason, and ExternalReference
    And the event is published only after the transaction row is committed
```


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
- **Idempotency Key**: `$"dispute-{transactionId}"`
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
