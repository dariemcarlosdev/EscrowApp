# 11 — Cancel Funds

> Voluntary cancellation of held escrow funds by mutual agreement.

## Overview

The **CancelFunds** feature allows a client or consultant to cancel an escrow hold before funds are released. Unlike a **Dispute** (which is adversarial — one party contests), a **Cancel** is cooperative — both parties agree to void the hold.

## State Machine

```
Pending ──→ Funded (Held) ──→ Cancelled    ← THIS FLOW
                           ──→ Released
                           ──→ Disputed ──→ (resolution path TBD)
```

**Key distinction:**
| Action | Who initiates | Why | Stripe operation |
|--------|---------------|-----|------------------|
| **Cancel** | Either party (cooperative) | Service no longer needed, scope change, mutual agreement | `PaymentIntent.Cancel` (void auth) |
| **Dispute** | One party (adversarial) | Quality disagreement, non-delivery, breach of terms | Hold remains — awaits resolution |

## MediatR Flow

```
CancelFundsCommand
├── TransactionId (int, required)
├── Reason (string, required)
├── IdempotencyKey (string, required)
└── CancelledBy (string, required — email of initiating party)

    ↓ Handler

1. Load transaction from IEscrowTransactionRepository
2. Validate: Status must be "Funded (Held)"
3. Resolve IFundCancellable from IPaymentStrategyFactory
4. Call CancelHoldAsync(externalReference, idempotencyKey)
5. Update Status → "Cancelled", persist via repository
6. Publish domain event (FundsCancelledEvent)
7. Return CancelFundsResult(success, transactionId)
```

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
| 200 | Hold voided successfully |
| 400 | Invalid state (not held) or missing fields |
| 404 | Transaction not found |
| 409 | Idempotency conflict |

## Stripe Integration

- Calls `PaymentIntentService.CancelAsync(paymentIntentId, options)` via `IFundCancellable`
- Stripe voids the authorization — no funds were ever captured
- Idempotency key ensures safe retries

## Files

| File | Purpose |
|------|---------|
| `Features/Escrow/CancelFunds/CancelFundsCommand.cs` | Command record |
| `Features/Escrow/CancelFunds/CancelFundsHandler.cs` | Handler with TODO implementation |
| `Features/Escrow/CancelFunds/CancelFundsResult.cs` | Result DTO |
| `Services/Strategies/IFundCancellable.cs` | Strategy interface (pre-existing) |

## Business Rules

1. Only transactions in **"Funded (Held)"** status can be cancelled
2. **Disputed** transactions cannot be cancelled — they follow a separate resolution path
3. **Released** transactions cannot be cancelled — funds already captured
4. Idempotency key is mandatory to prevent duplicate Stripe void calls
5. Both client and consultant should be notified of cancellation (future: email/webhook)

## Future Enhancements

- [ ] Require both parties to confirm cancellation (approval workflow)
- [ ] Partial cancellation (reduce held amount)
- [ ] Cancellation fee for late cancellations
- [ ] Automatic cancellation after configurable hold expiry
