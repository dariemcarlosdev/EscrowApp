# 02 — Escrow: Release Funds

> Capture (release) previously held funds to complete the escrow transaction.
> The consultant receives the payment once the client confirms delivery.

## Status: Implemented

---

## Overview

After a successful hold (see [Hold Funds](../hold-funds/hold-funds.md)),
the funds sit in a **requires_capture** state on Stripe. Releasing funds **captures** the
PaymentIntent, completing the transfer from the client to the platform. The handler reads
the stored `ExternalProvider` from the transaction to resolve the correct strategy at
runtime — ensuring provider-agnostic release logic.

## User Stories

Stories for capturing previously authorized funds via Stripe `capture`. **User-facing copy must use *release held funds* — never *release escrow*.** Disputes always block release.

### Story 1 — Client releases on delivery acceptance

**As a** Client, **I want** to release the held funds when the consultant has delivered the agreed service, **so that** the consultant is paid promptly and the engagement closes cleanly.

**Acceptance Criteria:**

- [ ] IFundReleasable.ReleaseFundsAsync is called with the stored ExternalReference
- [ ] the Stripe PaymentIntent is captured
- [ ] the transaction status transitions to "Completed (Released)"
- [ ] the platform fee is retained on the platform account

```gherkin
Feature: Capture on release
  Scenario: Successful release of a held transaction
    Given a transaction in status "Funded (Held)" with ExternalProvider="Stripe"
    When ReleaseFundsCommand is submitted
    Then IFundReleasable.ReleaseFundsAsync is called with the stored ExternalReference
    And the Stripe PaymentIntent is captured
    And the transaction status transitions to "Completed (Released)"
    And the platform fee is retained on the platform account
```

### Story 2 — Disputed transactions cannot be released

**As a** Compliance Officer, **I want** any transaction in `Disputed` status to be unconditionally ineligible for release, **so that** open disputes never collapse into a silent capture and the audit trail stays consistent.

**Acceptance Criteria:**

- [ ] the handler rejects the operation
- [ ] no Stripe capture call is made
- [ ] the API returns 409 Conflict

```gherkin
Feature: Dispute integrity
  Scenario: Release blocked when status = Disputed
    Given a transaction in status "Disputed"
    When ReleaseFundsCommand is submitted
    Then the handler rejects the operation
    And no Stripe capture call is made
    And the API returns 409 Conflict
```

### Story 3 — Strict status check before capture

**As a** Consultant, **I want** the release flow to fail fast if the transaction is not in the exact `Funded (Held)` state, **so that** I can never accidentally capture funds that were already cancelled, refunded, or never authorized.

**Acceptance Criteria:**

- [ ] the handler rejects with InvalidOperationException
- [ ] no Stripe call is made

```gherkin
Feature: Strict state validation
  Scenario: Release on a Pending transaction is rejected
    Given a transaction in status "Pending" (no hold yet)
    When ReleaseFundsCommand is submitted
    Then the handler rejects with InvalidOperationException
    And no Stripe call is made
```


## MediatR Command

```csharp
// File: Features/Escrow/ReleaseFunds/ReleaseFundsCommand.cs
public sealed record ReleaseFundsCommand(
    int TransactionId
) : IRequest<ReleaseFundsResult>;
```

## Result DTO

```csharp
// File: Features/Escrow/ReleaseFunds/ReleaseFundsResult.cs
public sealed record ReleaseFundsResult(
    int    TransactionId,
    string Status,
    bool   Success
);
```

## Handler Flow

```
UI ──Send(ReleaseFundsCommand)──► ReleaseFundsHandler
                                       │
                            1. Retrieve EscrowTransaction by ID
                            2. Reject if Status == "Disputed" (dispute blocks release)
                            3. Validate Status == "Held" (strict state check)
                            4. Validate ExternalReference and ExternalProvider exist
                            5. Resolve IFundReleasable strategy (from stored provider)
                            6. Call strategy.ReleaseFundsAsync(externalRef, idempotencyKey)
                            7. On success: Status = "Completed (Released)"
                            8. UpdateAsync(transaction)
                            9. Return ReleaseFundsResult
```

> **Security hardening (2026-04-11):** Steps 2–3 were added to enforce dispute integrity
> and strict status validation — a disputed transaction can never be released.

## Sequence Diagram

```
Client UI          MediatR           ReleaseFundsHandler     StripePaymentStrategy       Stripe API
    │                 │                     │                        │                       │
    │─Send(Command)──►│                     │                        │                       │
    │                 │──Handle(cmd, ct)───►│                        │                       │
    │                 │                     │──GetByIdAsync(id)─────►│(Repository)           │
    │                 │                     │◄─EscrowTransaction─────│                       │
    │                 │                     │                        │                       │
    │                 │                     │  Validate: ExternalRef │                       │
    │                 │                     │  + ExternalProvider set │                       │
    │                 │                     │                        │                       │
    │                 │                     │──ReleaseFundsAsync()──►│                        │
    │                 │                     │                        │──Capture PaymentIntent►│
    │                 │                     │                        │◄─status: "succeeded"───│
    │                 │                     │◄─true──────────────────│                       │
    │                 │                     │                        │                       │
    │                 │                     │──UpdateAsync(tx)──────►│(Repository)           │
    │                 │◄─ReleaseFundsResult──│                       │                       │
    │◄─Result─────────│                     │                        │                       │
```

## Status Transition

```
  ┌───────────────┐    ReleaseFunds    ┌──────────────────────┐
  │ Funded (Held) │ ──────────────────► │ Completed (Released) │
  └───────────────┘                     └──────────────────────┘
```

## Key Classes

| Class                      | File                                                | Responsibility                          |
| -------------------------- | --------------------------------------------------- | --------------------------------------- |
| `ReleaseFundsCommand`     | `Features/Escrow/ReleaseFunds/ReleaseFundsCommand.cs` | MediatR command — only needs TX ID     |
| `ReleaseFundsHandler`     | `Features/Escrow/ReleaseFunds/ReleaseFundsHandler.cs`  | Orchestrates capture flow              |
| `ReleaseFundsResult`      | `Features/Escrow/ReleaseFunds/ReleaseFundsResult.cs`   | Immutable result with success flag     |
| `StripePaymentStrategy`   | `Services/Strategies/StripePaymentStrategy.cs`          | Calls Stripe CaptureAsync              |

## Stripe Details

- **API Call**: `PaymentIntentService.CaptureAsync(externalReference)` with idempotency key
- **Success Check**: `PaymentIntent.Status == "succeeded"`
- **Idempotency Key**: `$"release-{transactionId}"` — prevents duplicate captures
- **Precondition**: PaymentIntent must be in `requires_capture` state

## Design Notes

- **No ProviderName parameter**: The handler reads `ExternalProvider` from the stored transaction.
  This guarantees the same provider that created the hold will release it.
- **Dispute blocks release**: The handler explicitly rejects any transaction with `Status == "Disputed"`.
  This is a fintech guardrail — once disputed, a transaction cannot be released without explicit resolution.
- **Strict status check**: Only transactions in `"Held"` status can be released. This prevents
  double-capture or releasing pending transactions.
- **No event published**: Unlike HoldFunds, the release flow does not currently publish a domain
  event. This is intentional for MVP — a `FundsReleasedEvent` can be added when webhook
  notifications to consultants are implemented.
- **Validation**: If `ExternalReference` or `ExternalProvider` is null/empty, the handler throws
  before calling Stripe — preventing invalid API calls.

## Error Handling

- Transaction status is `"Disputed"` → `InvalidOperationException` (dispute blocks release)
- Transaction status is not `"Held"` → `InvalidOperationException` (invalid state transition)
- Missing `ExternalReference` → `InvalidOperationException` (transaction was never held)
- Missing `ExternalProvider` → `InvalidOperationException` (cannot resolve strategy)
- Strategy not registered → `InvalidOperationException` from factory
- Stripe capture fails → exception propagates; status remains `"Funded (Held)"`
- Already captured → Stripe returns idempotent success (safe to retry)
