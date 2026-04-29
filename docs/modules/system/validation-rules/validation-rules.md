# 21 — Validation Rules Reference

> Complete reference for all input validation rules enforced across MediatR commands via FluentValidation pipeline behavior.

## Overview

All MediatR commands validate input **before** handler execution via `ValidationBehavior<TRequest, TResponse>`. Validation failures return HTTP 400 Bad Request with RFC 7807 ProblemDetails format.

---

## Command-by-Command Rules

### CreateAndHoldFundsCommand

Atomically create a transaction and place a hold in one operation.

| Field | Rules | Fintech Rationale |
|-------|-------|---|
| `Amount` | > 0, ≤ $500K | No zero payments; Stripe limit protection + fraud surface reduction |
| `ClientEmail` | NotEmpty, ValidEmailFormat | Notification routing; participant identification |
| `ConsultantEmail` | NotEmpty, ValidEmailFormat | Notification routing; participant identification |
| `ConsultantEmail` | ≠ `ClientEmail` | Prevents self-dealing (consultant cannot pay themselves) |
| `ServiceDescription` | NotEmpty, Max 500 chars | Audit trail completeness; clarity on services rendered |
| `PaymentMethodId` | NotEmpty | Required for Stripe PaymentIntent |
| `IdempotencyKey` | NotEmpty, Max 255 chars | **🔴 FINTECH GUARDRAIL** — Prevents duplicate charges on retry |
| `ProviderName` | Default "Stripe" | Supports future multi-provider strategy (PayPal, Ethereum) |

**Example Error Response:**
```json
{
  "status": 400,
  "title": "Validation Failed",
  "errors": {
    "Amount": ["Escrow amount must be greater than zero."],
    "ClientEmail": ["Client and consultant cannot be the same person."]
  }
}
```

---

### HoldFundsCommand

Place a hold on existing transaction without creating a new one.

| Field | Rules | Rationale |
|-------|-------|---|
| `TransactionId` | > 0 | Database entity ID validity |
| `PaymentMethodId` | NotEmpty | Required for Stripe PaymentIntent |
| `IdempotencyKey` | NotEmpty, Max 255 chars | **🔴 FINTECH GUARDRAIL** — Stripe retry safety |

---

### ReleaseFundsCommand

Capture held funds and complete the payment.

| Field | Rules | Rationale |
|-------|-------|---|
| `TransactionId` | > 0 | Database entity ID validity |
| `IdempotencyKey` | NotEmpty, Max 255 chars | **🔴 FINTECH GUARDRAIL** — Stripe retry safety |

---

### DisputeFundsCommand

Flag a transaction as disputed, blocking further operations.

| Field | Rules | Rationale |
|-------|-------|---|
| `TransactionId` | > 0 | Database entity ID validity |
| `Reason` | Min 10 chars, Max 1000 | Audit quality; prevents trivial disputes (e.g., "no") |
| `DisputedBy` | NotEmpty, ValidEmailFormat | Participant identification; audit trail |
| `IdempotencyKey` | NotEmpty, Max 255 chars | **🔴 FINTECH GUARDRAIL** — Stripe retry safety |

**Business rule:** Reason ≥ 10 characters enforces meaningful dispute documentation.

---

### CancelFundsCommand

Cancel escrow and void the hold by mutual agreement.

| Field | Rules | Rationale |
|-------|-------|---|
| `TransactionId` | > 0 | Database entity ID validity |
| `Reason` | Min 5 chars, Max 500 | Audit trail; simpler than dispute per MVP |
| `CancelledBy` | NotEmpty, ValidEmailFormat | Participant identification; audit trail |
| `IdempotencyKey` | NotEmpty, Max 255 chars | **🔴 FINTECH GUARDRAIL** — Stripe retry safety |

**Business rule:** Reason ≥ 5 characters (relaxed vs. Dispute's 10, reflecting lower operational severity).

---

## Error Response Format

All validation errors return HTTP 400 Bad Request with RFC 7807 ProblemDetails, grouped by property name:

```jsonc
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "PropertyName1": ["error message 1", "error message 2"],
    "PropertyName2": ["error message 3"]
  }
}
```

**Implementation:** `ApiExceptionMiddleware.cs` catches `ValidationException` and transforms it via `WriteValidationProblemDetails()`.

---

## Pipeline Architecture

```
┌─ HTTP POST /api/escrow/hold ─┐
│  X-Api-Key: ...              │
│  X-Idempotency-Key: ...      │
└──────────────┬────────────────┘
               ▼
        EscrowController
               │
               ├─ Extract headers
               ├─ Build CreateAndHoldFundsCommand
               │
               ▼
        IMediator.Send(command)
               │
               ▼ ValidationBehavior<CreateAndHoldFundsCommand>
               │
               ├─ For each IValidator<CreateAndHoldFundsCommand>
               │  ├─ Validate Amount, ClientEmail, ConsultantEmail, ...
               │  └─ Collect failures
               │
               ├─ If failures.Count > 0
               │  └─ Throw ValidationException(failures)
               │
               └─ Else (if valid)
                  └─ Handler (CreateAndHoldFundsHandler)
                     │
                     ├─ Persist EscrowTransaction
                     ├─ Call IPaymentStrategyFactory
                     ├─ HoldFunds via Stripe
                     ├─ Publish PaymentReceivedEvent
                     └─ Return EscrowTransactionResponse

         ▼ (Exception path)
    ApiExceptionMiddleware
         │
         ├─ Catch ValidationException
         ├─ Call WriteValidationProblemDetails()
         └─ Return 400 Bad Request with grouped errors
```

---

## Fintech Guardrails

### 🔴 Four Critical Validation Patterns

#### 1. Idempotency Key (All payment mutations)
- **Required on:** CreateAndHoldFunds, HoldFunds, ReleaseFunds, DisputeFunds, CancelFunds
- **Enforcement:** NotEmpty, Max 255 characters
- **Purpose:** Prevents duplicate charges when Stripe retries the PaymentIntent
- **Reference:** AGENTS.md → Fintech Rules → "Always use idempotency keys on all payment operations"

#### 2. Amount Limits
- **Rules:** > 0, ≤ $500K
- **Enforcement:** Only on CreateAndHoldFunds
- **Purpose:** Fraud surface reduction + Stripe authorization limit protection
- **Future:** Post-MVP may allow per-customer limits or tiered thresholds

#### 3. Email Validation
- **Rules:** NotEmpty, ValidEmailFormat, mutual exclusivity (client ≠ consultant)
- **Enforcement:** On CreateAndHoldFunds, DisputeFunds, CancelFunds
- **Purpose:** Ensures notification routing works + prevents self-dealing
- **Implementation:** Follows ASP.NET Core EmailAddress() validator (RFC 5322 subset)

#### 4. Reason Enforcement
- **Rules:** MinimumLength (Dispute: 10, Cancel: 5), MaximumLength (Dispute: 1000, Cancel: 500)
- **Enforcement:** On DisputeFunds, CancelFunds
- **Purpose:** Audit trail quality + prevents trivial disputes
- **Rationale:** Dispute (higher severity) requires more detail than Cancel

---

## Testing Strategy

See `docs/cross-cutting/testing/testing-strategy.md` for comprehensive test coverage guidance.

**Quick reference for validator tests:**

```csharp
// Using FluentValidation.TestHelper
[Fact]
public async Task Validate_AmountGreaterThanZero_Passes()
{
    var command = new CreateAndHoldFundsCommand(
        clientEmail: "client@ex.com",
        consultantEmail: "consultant@ex.com",
        amount: 100m,
        serviceDescription: "Consulting",
        paymentMethodId: "pm_card_visa",
        idempotencyKey: "key-123");

    var result = await validator.TestValidateAsync(command);
    
    result.ShouldNotHaveValidationErrorFor(x => x.Amount);
}

[Fact]
public async Task Validate_AmountZero_FailsWithMessage()
{
    var command = new CreateAndHoldFundsCommand(
        clientEmail: "client@ex.com",
        consultantEmail: "consultant@ex.com",
        amount: 0m,
        serviceDescription: "Consulting",
        paymentMethodId: "pm_card_visa",
        idempotencyKey: "key-123");

    var result = await validator.TestValidateAsync(command);
    
    result
        .ShouldHaveValidationErrorFor(x => x.Amount)
        .WithErrorMessage("*greater than zero*");
}
```

**Pattern:** One test per validation rule (positive + negative paths).

---

## Files & Implementation

| File | Created | Purpose |
|------|---------|---------|
| `Features/Behaviors/ValidationBehavior.cs` | ✅ | Auto-applies validators to all commands |
| `Features/Escrow/*/[Command]Validator.cs` | ✅ | 5 validators with 30+ total rules |
| `Infrastructure/Middleware/ApiExceptionMiddleware.cs` | ✅ Modified | Catches ValidationException → 400 |
| `Program.cs` | ✅ Modified | Registers `AddValidatorsFromAssemblyContaining<Program>()` |

---

## Related Documentation

- **architecture/api-integration** — REST endpoint specifications with error examples
- **architecture/overview** — System design showing ValidationBehavior in pipeline
- **cross-cutting/testing** — Complete test strategy and patterns
- **features/input-validation** — Detailed validator implementation guides
