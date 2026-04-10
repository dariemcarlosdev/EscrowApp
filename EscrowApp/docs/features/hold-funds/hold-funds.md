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
- **Return URL**: `http://localhost:5222/payment-return` (3D Secure redirect)
- **Hold Duration**: Stripe holds expire after **7 days** if not captured or cancelled

## Error Handling

- Transaction not found → exception from repository
- Strategy not registered → `InvalidOperationException` from `PaymentStrategyFactory`
- Provider doesn't support holds → `NotSupportedException`
- Stripe API failure → exception propagates to UI for handling
