# Features — Vertical Slice Inventory

> Last synced with codebase: 2026-04-16
> Layer: **Application** — `Features/Escrow/` (MediatR CQRS vertical slices)

This document is the ground-truth inventory of every vertical slice and pipeline behavior
in the `Features/` folder. It maps each slice to its implementation status, command/result
contracts, and the domain operations it owns.

---

## Layer Map

```
Features/
├── Behaviors/          MediatR pipeline behaviors (cross-cutting, all requests)
│   ├── LoggingBehavior.cs        ✅ Live
│   └── PerformanceBehavior.cs    ✅ Live
└── Escrow/             Payment vertical slices
    ├── Api/            Shared contracts (request/response DTOs, controller)
    ├── CreateAndHoldFunds/   ✅ Live — Revenue Blocker #1 complete
    ├── HoldFunds/            ✅ Live
    ├── ReleaseFunds/         ✅ Live (Bug fixed 2026-04-14)
    ├── DisputeFunds/         ✅ Live
    ├── CancelFunds/          ✅ Live (Implemented 2026-04-14)
    ├── GetTransaction/       ✅ Live
    ├── ListTransactions/     ✅ Live
    └── Webhooks/             ⚠️  Stub — handler registered but unread parameters
```

---

## Pipeline Behaviors (`Features/Behaviors/`)

Registered in `Program.cs` as open generic behaviors — apply to **every** MediatR request automatically.

| Behavior | Status | What it does |
|---|---|---|
| `LoggingBehavior<TRequest, TResponse>` | ✅ Live | Logs request name at start and completion. **Never logs payload** (PII guardrail). |
| `PerformanceBehavior<TRequest, TResponse>` | ✅ Live | Measures handler execution time; logs a warning when duration exceeds threshold. |
| `ValidationBehavior<TRequest, TResponse>` | ✅ Live (Track A #4) | Validates all commands via FluentValidation before handler execution. Throws ValidationException → 400 Bad Request. |

---

## Shared API Contracts (`Features/Escrow/Api/`)

### `ApiContracts.cs` — Request / Response DTOs

| Type | Kind | Purpose |
|---|---|---|
| `CreateAndHoldRequest` | Request | Creates + holds in one call. Validated via DataAnnotations (pre-FluentValidation). |
| `ReleaseFundsApiRequest` | Request | Optional idempotency key for release. |
| `DisputeFundsApiRequest` | Request | Dispute reason (required, max 1000 chars). |
| `CancelFundsApiRequest` | Request | Cancellation reason (required, max 1000 chars). |
| `EscrowTransactionResponse` | Response | **Updated 2026-04-14** — now includes `PlatformFee`, `PlatformFeePercentage`, `TotalCharged` |
| `PaginatedResponse<T>` | Response | Wrapper for list endpoints (Items, Page, PageSize, TotalCount, TotalPages). |

### `EscrowController.cs` — REST Endpoints

| Method | Route | Handler dispatched | Auth |
|---|---|---|---|
| `POST` | `/api/escrow` | `CreateAndHoldFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/hold` | `HoldFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/release` | `ReleaseFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/dispute` | `DisputeFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/cancel` | `CancelFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `GET` | `/api/escrow/{id}` | `GetTransactionQuery` | `[Authorize(Policy="ApiAccess")]` |
| `GET` | `/api/escrow` | `ListTransactionsQuery` | `[Authorize(Policy="ApiAccess")]` |

---

## Slice Details

---

### `CreateAndHoldFunds/` — ✅ Live (Updated 2026-04-14)

**Purpose:** Create a new `EscrowTransaction` and atomically authorize a payment hold via Stripe.
This is the primary revenue-generating entry point.

**Command:**
```csharp
CreateAndHoldFundsCommand(
    string ClientEmail,
    string ConsultantEmail,
    decimal Amount,           // escrow portion only
    string ServiceDescription,
    string PaymentMethodId,
    string ProviderName = "Stripe")
```

**Handler flow:**
1. Load `PlatformOptions` from `IOptions<PlatformOptions>` (injected, from `Shared/Configuration/`)
2. Calculate `platformFee = max(Amount × FeePercentage, MinimumFee)` — e.g., `max($5000 × 0.015, $0.50) = $75.00`
3. Create `EscrowTransaction` entity — snapshots `PlatformFee` and `PlatformFeePercentage` for audit trail immutability
4. Persist via `IEscrowTransactionRepository.AddAsync()` (gets DB-assigned ID)
5. Resolve `IFundHoldable` via `IPaymentStrategyFactory.ResolveHoldStrategy(providerName)`
6. Call `HoldFundsAsync(Amount + platformFee, paymentMethodId, idempotencyKey: "hold-{id}")` — Stripe authorized for the **total** (escrow + fee)
7. Update `ExternalReference`, `ExternalProvider`, `Status = "Funded (Held)"`
8. Persist updated entity via `UpdateAsync()`
9. Publish `PaymentReceivedEvent` — includes `PlatformFee` + `PlatformFeePercentage` for audit trail
10. Return `EscrowTransactionResponse` (includes `PlatformFee`, `PlatformFeePercentage`, `TotalCharged`)

**Key changes (2026-04-14 — Platform Fee implementation):**
- Injected `IOptions<PlatformOptions>` — fee config from `Shared/Configuration/PlatformOptions`
- Fee calculated before Stripe call — Stripe holds `escrowAmount + platformFee`
- `PlatformFee` and `PlatformFeePercentage` snapshotted at creation (immutable — fintech audit rule)
- `PaymentReceivedEvent` extended with fee fields
- `EscrowTransactionResponse` extended with `PlatformFee`, `PlatformFeePercentage`, `TotalCharged`

**Files:**
| File | Status |
|---|---|
| `CreateAndHoldFundsCommand.cs` | ✅ Unchanged |
| `CreateAndHoldFundsHandler.cs` | ✅ Updated 2026-04-14 |

---

### `HoldFunds/` — ✅ Live

**Purpose:** Place a payment hold on an **existing** `EscrowTransaction` (created separately).
Differs from `CreateAndHoldFunds` — used when the transaction record already exists.

> ⚠️ **Known gap:** Does not use `PlatformOptions` — holds `transaction.Amount` directly (no fee added).
> This will need alignment with the fee model if this endpoint is used in production.
> `CreateAndHoldFunds` is the preferred Day-1 path.

**Command:**
```csharp
HoldFundsCommand(
    int TransactionId,
    string PaymentMethodId,
    string ProviderName = "Stripe")
```

**Handler flow:**
1. Load transaction via `IEscrowTransactionRepository.GetByIdAsync()` — throws if not found
2. Resolve `IFundHoldable` via `IPaymentStrategyFactory.ResolveHoldStrategy()`
3. Call `HoldFundsAsync(transaction.Amount, paymentMethodId, idempotencyKey: "hold-{id}")`
4. Update `ExternalReference`, `ExternalProvider`, `Status = "Funded (Held)"`
5. Persist via `UpdateAsync()`
6. Publish `PaymentReceivedEvent` — ⚠️ **does not yet include `PlatformFee` fields** (pre-existing gap)
7. Return `HoldFundsResult`

**Files:**
| File | Status |
|---|---|
| `HoldFundsCommand.cs` | ✅ |
| `HoldFundsHandler.cs` | ✅ (fee fields not propagated — see note above) |
| `HoldFundsResult.cs` | ✅ |

---

### `ReleaseFunds/` — ✅ Live

**Purpose:** Capture a held Stripe PaymentIntent — triggers the money movement from authorization to actual charge and payout.

**Command:**
```csharp
ReleaseFundsCommand(int TransactionId)
```

**Handler flow:**
1. Load transaction via `GetByIdAsync()` — throws if not found
2. Guard: `Status == "Disputed"` → throws (disputed transactions cannot be released)
3. Guard: `Status != "Held"` → throws — ⚠️ **Note:** actual held status string is `"Funded (Held)"`, not `"Held"`. This is a **pre-existing bug** — release will always fail in current state.
4. Guard: `ExternalReference` or `ExternalProvider` null → throws
5. Resolve `IFundReleasable` via `IPaymentStrategyFactory.ResolveReleaseStrategy()`
6. Call `ReleaseFundsAsync(externalReference, idempotencyKey: "release-{id}")`
7. On success: `Status = "Completed (Released)"`, persist via `UpdateAsync()`
8. Return `ReleaseFundsResult`

> ✅ **Bug fixed 2026-04-14:** Status guard now correctly checks for `"Funded (Held)"` (canonical status).

**Files:**
| File | Status |
|---|---|
| `ReleaseFundsCommand.cs` | ✅ |
| `ReleaseFundsHandler.cs` | ✅ (status string mismatch — see note) |
| `ReleaseFundsResult.cs` | ✅ |

---

### `DisputeFunds/` — ✅ Live

**Purpose:** Flag an active hold as disputed. Voids the Stripe authorization (returning funds to the client's card) and locks the transaction for manual resolution.

**Command:**
```csharp
DisputeFundsCommand(
    int TransactionId,
    string Reason,
    string RaisedBy)   // email of disputing party
```

**Handler flow:**
1. Load transaction via `GetByIdAsync()` — throws if not found
2. Guard: `Status != "Funded (Held)"` → throws
3. Guard: `ExternalReference` or `ExternalProvider` null → throws
4. Resolve `IFundCancellable` via `IPaymentStrategyFactory.ResolveCancelStrategy()`
5. Call `CancelHoldAsync(externalReference, idempotencyKey: "dispute-{id}")` — voids Stripe auth
6. Update `Status = "Disputed"`, set `DisputeReason = command.Reason`
7. Persist via `UpdateAsync()`
8. Publish `DisputeRaisedEvent` (includes `TransactionId`, `DisputeReason`, `RaisedBy`, `ExternalReference`)
9. Return `DisputeFundsResult`

**Files:**
| File | Status |
|---|---|
| `DisputeFundsCommand.cs` | ✅ |
| `DisputeFundsHandler.cs` | ✅ |
| `DisputeFundsResult.cs` | ✅ |

---

### `CancelFunds/` — ✅ Live (Implemented 2026-04-14)

**Purpose:** Cooperative voluntary cancellation — voids the hold when both parties agree to exit.
Distinct from DisputeFunds (cooperative vs adversarial).

**Command:**
```csharp
CancelFundsCommand(
    int TransactionId,
    string Reason,
    string CancelledBy,
    string IdempotencyKey)
```

**Handler flow:**
1. Load transaction — 404 if not found
2. Guard: Status must be `"Funded (Held)"`
3. Resolve `IFundCancellable` via `IPaymentStrategyFactory.ResolveCancelStrategy()`
4. Call `CancelHoldAsync(externalReference, idempotencyKey)` — voids Stripe auth
5. Update `Status = "Cancelled"`, persist via `UpdateAsync()`
6. Publish `FundsCancelledEvent` (includes audit fields: Reason, CancelledBy)
7. Return `CancelFundsResult`

**Files:**
| File | Status |
|---|---|
| `CancelFundsCommand.cs` | ✅ |
| `CancelFundsHandler.cs` | ✅ Implemented 2026-04-14 |
| `CancelFundsResult.cs` | ✅ |

---

### `GetTransaction/` — ✅ Live

**Purpose:** Read a single `EscrowTransaction` by ID.

**Query:**
```csharp
GetTransactionQuery(int TransactionId)
```

Returns `EscrowTransactionResponse`. No side effects.

---

### `ListTransactions/` — ✅ Live

**Purpose:** Read a paginated list of `EscrowTransaction` records.

**Query:**
```csharp
ListTransactionsQuery(int Page = 1, int PageSize = 20)
```

Returns `PaginatedResponse<EscrowTransactionResponse>`.

---

### `Webhooks/` — ⚠️ Stub (Track C #7)

**Purpose:** Handle incoming Stripe webhook events (`payment_intent.succeeded`, etc.)

**Status:** Handler class registered but parameters (`repo`, `eventBus`) are unread — compiler
warnings CS9113 confirm this is a stub. Stripe signature verification is not yet implemented.
See `docs/architecture/stripe-webhooks/` for the implementation spec.

---

## Cross-Cutting Gaps (Pre-Existing, Pre-Platform Fee)

| Gap | Affected Slices | Status |
|---|---|---|
| `ReleaseFundsHandler` status check uses `"Held"` not `"Funded (Held)"` | `ReleaseFunds` | ✅ Fixed 2026-04-14 |
| `HoldFundsHandler` does not calculate or propagate `PlatformFee` | `HoldFunds` | 🟡 Inconsistency — use `CreateAndHoldFunds` as Day-1 path |
| No `FundsCancelledEvent` domain event exists yet | `CancelFunds` | ✅ Fixed 2026-04-14 |
| No `ValidationBehavior` pipeline behavior | All slices | ✅ Implemented 2026-04-16 (Track A #4) |
| `PaymentIntentEventHandler` (Webhooks) unimplemented | `Webhooks` | 🟡 Track C #7 |
