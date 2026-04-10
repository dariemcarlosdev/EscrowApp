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
                            2. Validate ExternalReference and ExternalProvider exist
                            3. Resolve IFundReleasable strategy (from stored provider)
                            4. Call strategy.ReleaseFundsAsync(externalRef, idempotencyKey)
                            5. On success: Status = "Completed (Released)"
                            6. UpdateAsync(transaction)
                            7. Return ReleaseFundsResult
```

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
- **No event published**: Unlike HoldFunds, the release flow does not currently publish a domain
  event. This is intentional for MVP — a `FundsReleasedEvent` can be added when webhook
  notifications to consultants are implemented.
- **Validation**: If `ExternalReference` or `ExternalProvider` is null/empty, the handler throws
  before calling Stripe — preventing invalid API calls.

## Error Handling

- Missing `ExternalReference` → `InvalidOperationException` (transaction was never held)
- Missing `ExternalProvider` → `InvalidOperationException` (cannot resolve strategy)
- Strategy not registered → `InvalidOperationException` from factory
- Stripe capture fails → exception propagates; status remains `"Funded (Held)"`
- Already captured → Stripe returns idempotent success (safe to retry)
