# 01 — Escrow: Hold Funds

> Place a payment hold on an escrow transaction. Funds are authorized but not captured,
> giving both parties a secure holding period before release.

## Status: Implemented

---

## Overview

When a client initiates an escrow payment, a **hold** is placed on their payment method.
The funds are authorized (reserved) but **not yet captured** — the consultant cannot
access them until the hold is explicitly released. This follows Stripe's
**manual capture** flow via `PaymentIntent` with `CaptureMethod = "manual"`.

## User Stories

Stories for placing a Stripe manual-capture hold on an existing transaction. **User-facing copy must use *secure payment holding* / *held funds* — never *escrow*.**

### Story 1 — Client authorizes a hold

**As a** Client, **I want** my payment method to be authorized (but not yet captured) when I commit funds for a service, **so that** the consultant has a credible signal of intent without me actually paying until they deliver.

**Acceptance Criteria:**

- [ ] the handler resolves IFundHoldable for "Stripe"
- [ ] HoldFundsAsync is called with the transaction amount and an idempotency key
- [ ] ExternalReference, ExternalProvider, and Status are updated
- [ ] Status becomes "Funded (Held)"
- [ ] PaymentReceivedEvent is published after persistence

```gherkin
Feature: Authorize a hold via Stripe manual capture
  Scenario: Successful hold on a Pending transaction
    Given a transaction exists in status "Pending" with a known amount
    And the client has a valid Stripe PaymentMethod
    When HoldFundsCommand is submitted with TransactionId, PaymentMethodId, and ProviderName="Stripe"
    Then the handler resolves IFundHoldable for "Stripe"
    And HoldFundsAsync is called with the transaction amount and an idempotency key
    Then ExternalReference, ExternalProvider, and Status are updated
    And Status becomes "Funded (Held)"
    And PaymentReceivedEvent is published after persistence
```

### Story 2 — Idempotent retries are safe

**As a** Developer, **I want** the hold operation to be idempotent on the same `IdempotencyKey`, **so that** UI retries or duplicate webhook-triggered calls never authorize twice on the client's card.

**Acceptance Criteria:**

- [ ] Stripe returns the same PaymentIntent (no duplicate authorization)
- [ ] the transaction state is not regressed

```gherkin
Feature: Stripe idempotency
  Scenario: Same idempotency key replayed
    Given a HoldFundsCommand has succeeded with key "hold-42-v1"
    When the same command is replayed with the same key
    Then Stripe returns the same PaymentIntent (no duplicate authorization)
    And the transaction state is not regressed
```

### Story 3 — Manual capture, never auto-capture

**As a** Compliance Officer, **I want** every hold to use Stripe manual capture (`CaptureMethod = "manual"`), **so that** funds are reservable but not transferable to the platform until an explicit Release step backed by an audit event.

**Acceptance Criteria:**

- [ ] CaptureMethod is "manual"
- [ ] the funds are in "requires_capture" until ReleaseFunds is invoked

```gherkin
Feature: Manual capture is enforced
  Scenario: PaymentIntent capture method
    When HoldFundsAsync creates the Stripe PaymentIntent
    Then CaptureMethod is "manual"
    And the funds are in "requires_capture" until ReleaseFunds is invoked
```


## MediatR Command

```csharp
// File: Features/Escrow/HoldFunds/HoldFundsCommand.cs
public sealed record HoldFundsCommand(
    int    TransactionId,
    string PaymentMethodId,
    string ProviderName = "Stripe"
) : IRequest<HoldFundsResult>;
```

## Result DTO

```csharp
// File: Features/Escrow/HoldFunds/HoldFundsResult.cs
public sealed record HoldFundsResult(
    int     TransactionId,
    string  Status,
    string  ExternalReference,   // Stripe PaymentIntent ID or crypto tx hash
    string  ExternalProvider,    // "Stripe", "PayPal", "Ethereum"
    decimal Amount
);
```

## Handler Flow

```
UI ──Send(HoldFundsCommand)──► HoldFundsHandler
                                   │
                        1. Retrieve EscrowTransaction by ID
                        2. Resolve IFundHoldable strategy (by ProviderName)
                        3. Call strategy.HoldFundsAsync(amount, paymentMethodId, idempotencyKey)
                        4. Update transaction:
                           ├── ExternalReference = Stripe PaymentIntent ID
                           ├── ExternalProvider  = "Stripe"
                           └── Status            = "Funded (Held)"
                        5. Publish PaymentReceivedEvent via IEventBus
                        6. Return HoldFundsResult to UI
```

## Sequence Diagram

```
Client UI          MediatR           HoldFundsHandler       StripePaymentStrategy       Stripe API
    │                 │                     │                        │                       │
    │─Send(Command)──►│                     │                        │                       │
    │                 │──Handle(cmd, ct)───►│                        │                       │
    │                 │                     │──GetByIdAsync(id)─────►│(Repository)           │
    │                 │                     │◄─EscrowTransaction─────│                       │
    │                 │                     │                        │                       │
    │                 │                     │──HoldFundsAsync()────►│                        │
    │                 │                     │                        │──Create PaymentIntent─►│
    │                 │                     │                        │  CaptureMethod=manual  │
    │                 │                     │                        │◄─PaymentIntent.Id──────│
    │                 │                     │◄─externalRef───────────│                       │
    │                 │                     │                        │                       │
    │                 │                     │──UpdateAsync(tx)──────►│(Repository)           │
    │                 │                     │──PublishAsync(event)──►│(EventBus)             │
    │                 │◄─HoldFundsResult────│                        │                       │
    │◄─Result─────────│                     │                        │                       │
```

## Status Transition

```
  ┌─────────┐    HoldFunds     ┌───────────────┐
  │ Pending │ ────────────────► │ Funded (Held) │
  └─────────┘                   └───────────────┘
```

## Key Classes

| Class                      | File                                          | Responsibility                         |
| -------------------------- | --------------------------------------------- | -------------------------------------- |
| `HoldFundsCommand`        | `Features/Escrow/HoldFunds/HoldFundsCommand.cs` | MediatR command with input params     |
| `HoldFundsHandler`        | `Features/Escrow/HoldFunds/HoldFundsHandler.cs`  | Orchestrates hold flow                |
| `HoldFundsResult`         | `Features/Escrow/HoldFunds/HoldFundsResult.cs`   | Immutable result DTO                  |
| `StripePaymentStrategy`   | `Services/Strategies/StripePaymentStrategy.cs`    | Creates PaymentIntent (manual capture)|
| `PaymentReceivedEvent`    | `Events/PaymentReceivedEvent.cs`                  | Domain event emitted on success       |

---

## Related: Create and Hold (Atomic)

The `CreateAndHoldFunds` slice combines transaction creation and hold into a single
atomic operation — designed for the REST API where callers should not need to make
two separate calls.

### Command

```csharp
// File: Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsCommand.cs
public sealed record CreateAndHoldFundsCommand(
    string  ClientEmail,
    string  ConsultantEmail,
    decimal Amount,
    string  ServiceDescription,
    string  PaymentMethodId,
    string  ProviderName = "Stripe"
) : IRequest<EscrowTransactionResponse>;
```

### Flow

```
API ──POST /api/escrow/hold──► CreateAndHoldFundsHandler
                                    │
                         1. Create new EscrowTransaction entity
                         2. Resolve IFundHoldable strategy (by ProviderName)
                         3. Call strategy.HoldFundsAsync(amount, paymentMethodId, idempotencyKey)
                         4. Update transaction with external reference + status
                         5. Persist via repository.AddAsync()
                         6. Publish PaymentReceivedEvent via IEventBus
                         7. Return EscrowTransactionResponse
```

### Key Files

| Class                            | File                                                         | Responsibility                        |
| -------------------------------- | ------------------------------------------------------------ | ------------------------------------- |
| `CreateAndHoldFundsCommand`     | `Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsCommand.cs` | Combined create + hold command   |
| `CreateAndHoldFundsHandler`     | `Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsHandler.cs` | Orchestrates atomic create + hold|

## Stripe Details

- **API Call**: `PaymentIntentService.CreateAsync()` with `CaptureMethod = "manual"`, `Confirm = true`
- **Amount Conversion**: `decimal amount × 100` → cents (Stripe uses smallest currency unit)
- **Idempotency**: Every request includes an idempotency key (`$"hold-{transactionId}"`)
- **Return URL**: Configurable via `Stripe:PaymentReturnUrl` in `appsettings.json` (used for 3D Secure redirect). Throws `InvalidOperationException` if not configured.
- **Hold Duration**: Stripe holds expire after **7 days** if not captured or cancelled

## Error Handling

- Transaction not found → exception from repository
- Strategy not registered → `InvalidOperationException` from `PaymentStrategyFactory`
- Provider doesn't support holds → `NotSupportedException`
- Stripe API failure → exception propagates to UI for handling
