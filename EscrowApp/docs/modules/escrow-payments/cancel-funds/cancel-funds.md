# 11 — Cancel Funds

> ✅ **Implemented — 2026-04-14**
> ⚠️ Compliance-sensitive — requires legal review before production deployment.
> Cancellation voids the Stripe authorization entirely — platform fee is **not collected** on cancelled transactions.

## Overview

The **CancelFunds** feature allows a client or consultant to cancel a payment hold before funds are released. Unlike a **Dispute** (adversarial — one party contests), a **Cancel** is cooperative — both parties agree to void the hold.

**Revenue impact:** On cancellation, Stripe voids the entire authorization including the platform fee component. NexTruzt.io collects **no revenue** on cancelled transactions. This is by design — it is a key trust differentiator: clients know they can exit a bad engagement cleanly.

## State Machine

```
Pending ──→ Funded (Held) ──→ Cancelled    ← THIS FLOW
                           ──→ Released
                           ──→ Disputed ──→ (resolution path TBD)
```

**Key distinction:**

| Action | Who initiates | Why | Stripe operation |
|--------|---------------|-----|------------------|
| **Cancel** | Either party (cooperative) | Service no longer needed, scope change | `PaymentIntent.Cancel` (void auth) |
| **Dispute** | One party (adversarial) | Quality disagreement, non-delivery | Hold cancelled — awaits resolution |

---

## Implementation Checklist (Track A #2) — ✅ DONE 2026-04-14

- [x] Replace `NotImplementedException` in `CancelFundsHandler` with real orchestration logic
- [x] Call `IFundCancellable.CancelHoldAsync(externalRef, idempotencyKey, ct)` via `IPaymentStrategyFactory.ResolveCancelStrategy()`
- [x] Update `EscrowTransaction.Status` → `"Cancelled"` via `IEscrowTransactionRepository.UpdateAsync`
- [x] Create `FundsCancelledEvent` domain event in `Events/`
- [x] Publish `FundsCancelledEvent` via `IEventBus` **after** successful persistence (architecture rule)
- [x] **Bonus fix:** `ReleaseFundsHandler` status string corrected `"Held"` → `"Funded (Held)"` (pre-existing bug, fixed same session)

---

## MediatR Flow (Real Implementation)

```
CancelFundsCommand
├── TransactionId (int, required)
├── Reason (string, required, min 5 chars)
├── IdempotencyKey (string, required)
└── CancelledBy (string, required — email of initiating party)

    ↓ ValidationBehavior (FluentValidation)
    ↓ CancelFundsHandler

1. Load transaction: IEscrowTransactionRepository.GetByIdAsync(command.TransactionId)
2. Guard: transaction not found → throw NotFoundException (→ 404)
3. Guard: Status != "Funded (Held)" → throw InvalidOperationException (→ 409 Conflict)
4. Guard: Status == "Disputed" → throw InvalidOperationException with specific message
5. Resolve: IPaymentStrategyFactory.GetStrategy<IFundCancellable>()
6. Call: await strategy.CancelHoldAsync(transaction.ExternalReference, command.IdempotencyKey, cancellationToken)
7. Mutate: transaction.Status = "Cancelled", transaction.UpdatedAt = DateTimeOffset.UtcNow
8. Persist: await _repository.UpdateAsync(transaction, cancellationToken)
9. Publish: await _eventBus.PublishAsync(new FundsCancelledEvent(...), cancellationToken)
10. Return: new CancelFundsResult(Success: true, TransactionId: transaction.Id)
```

---

## Domain Event

```csharp
// Events/FundsCancelledEvent.cs
public sealed record FundsCancelledEvent(
    int TransactionId,
    decimal EscrowAmount,
    string Reason,
    string CancelledBy,
    DateTimeOffset OccurredAt) : DomainEvent;
```

> **Audit rule:** `Reason` and `CancelledBy` are required in the event payload for regulatory traceability — even though no revenue is generated, the cancellation must be traceable per `AGENTS.md` fintech guardrails.

---

## API Endpoint

```http
POST /api/escrow/{id}/cancel
Content-Type: application/json
X-Api-Key: {api-key}

{
  "reason": "Service scope changed — client and consultant agreed to cancel",
  "cancelledBy": "client@example.com",
  "idempotencyKey": "cancel-txn-42-uuid"
}
```

**Responses:**

| Code | Meaning |
|------|---------|
| 200 | Hold voided successfully — Stripe authorization cancelled |
| 400 | Validation failure (missing fields, invalid email) |
| 404 | Transaction not found |
| 409 | Invalid state — transaction is not in "Funded (Held)" status |

---

## Stripe Integration

- Calls `PaymentIntentService.CancelAsync(paymentIntentId, options)` via `IFundCancellable`
- Stripe voids the authorization immediately — no funds were ever captured or collected
- Idempotency key ensures safe retries (Stripe native idempotency support)
- If Stripe returns `payment_intent_unexpected_state`, the handler must treat this as idempotent success (already cancelled)

---

## Fintech Guardrails

1. **Idempotency key required** — cancelling a PaymentIntent twice would throw a Stripe error without it
2. **Status guard before Stripe call** — never call Stripe if the transaction is already Cancelled, Released, or Disputed
3. **Persist before event** — `UpdateAsync` must succeed before `PublishAsync`. Domain events reflect committed state.
4. **No amount modification** — the cancellation voids the full authorization including platform fee; no partial amounts

---

## Files

| File | Action | Purpose |
|------|--------|---------|
| `Features/Escrow/CancelFunds/CancelFundsCommand.cs` | Existing | Command record |
| `Features/Escrow/CancelFunds/CancelFundsHandler.cs` | **Modify** | Replace `NotImplementedException` with real logic |
| `Features/Escrow/CancelFunds/CancelFundsResult.cs` | Existing | Result DTO |
| `Features/Escrow/CancelFunds/CancelFundsCommandValidator.cs` | **Create** | FluentValidation validator |
| `Events/FundsCancelledEvent.cs` | **Create** | Domain event record |
| `Services/Strategies/IFundCancellable.cs` | Existing | Strategy interface (no change) |

---

## Business Rules

1. Only transactions in **"Funded (Held)"** status can be cancelled
2. **Disputed** transactions cannot be cancelled — they follow a separate resolution path
3. **Released** transactions cannot be cancelled — funds already captured and sent
4. Idempotency key is mandatory to prevent duplicate Stripe void calls
5. **No platform fee is collected** on cancelled transactions — this is intentional (trust design)
6. Both client and consultant should be notified of cancellation (post-MVP: email/webhook notifications)

---

## Testing Notes (4 Required Tests)

```
CancelFundsHandlerTests
├── Handle_Should_CancelHold_And_UpdateStatus_When_TransactionIsHeld
├── Handle_Should_PublishFundsCancelledEvent_On_Success
├── Handle_Should_Throw_NotFoundException_When_TransactionNotFound
└── Handle_Should_Throw_InvalidOperationException_When_TransactionIsNotHeld
```

Each test uses Moq for `IEscrowTransactionRepository`, `IPaymentStrategyFactory`, and `IEventBus`. See `docs/cross-cutting/testing/` for the full test strategy.

---

## Future Enhancements (Post-MVP Backlog)

| Enhancement | Trigger |
|---|---|
| Require both parties to confirm cancellation (approval workflow) | User feedback |
| Cancellation fee for late cancellations (e.g., < 24hr before start date) | Business decision |
| Automatic cancellation after configurable hold expiry | Stripe Connect architecture migration |
| Partial cancellation (reduce held amount, re-auth for remainder) | Milestone payment feature (v1.2) |
