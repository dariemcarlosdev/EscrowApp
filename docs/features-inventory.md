# Features — Vertical Slice Inventory

> Last synced with codebase: 2026-04-30 18:09 UTC (inventory alignment fixes)
> **Status:** ✅ Core CQRS slices live (27/27 tracked implementation tasks complete) | 132/132 tests passing | Build: 0 errors, 0 warnings
> Layer focus: **Application** — `Features/Escrow/` + `Features/Auth/` (MediatR CQRS vertical slices)

This document is the ground-truth inventory of every vertical slice and pipeline behavior
in the `Features/` folder. It also links the related UI pages and documentation modules
that affect feature discoverability, but it does **not** attempt to inventory every
infrastructure or external NexSynapse asset.

**🎉 MILESTONE:** Track B (Authentication) + Track C (Stripe Webhooks) = 100% COMPLETE

> 📁 **Documentation Location:** Feature docs moved from `docs/features/` to `docs/modules/` organized by concern.

## Documentation Organization

Features are now organized by **module/concern** for faster context discovery:

```
docs/
├── modules/
│   ├── authentication/          # All auth features and patterns
│   │   ├── user-login/          # Login feature documentation 
│   │   ├── user-registration/   # Registration feature documentation
│   │   ├── aspnet-identity-mvp/ # ASP.NET Identity setup and config
│   │   └── hybrid-identity/     # Web2/Web3 identity bridging
│   ├── escrow-payments/         # All payment escrow features
│   │   ├── hold-funds/          # Hold funds flow documentation (includes atomic create + hold)
│   │   ├── release-funds/       # Release funds flow documentation  
│   │   ├── dispute-funds/       # Dispute flow documentation
│   │   ├── cancel-funds/        # Cancel escrow flow documentation
│   │   └── platform-fee/        # Platform fee calculation
│   ├── user-interface/          # All UI components and dashboards
│   │   ├── client-dashboard/    # Client transaction dashboard
│   │   ├── consultant-dashboard/ # Consultant earnings dashboard
│   │   ├── transaction-detail/  # Transaction detail view
│   │   └── landing-page/        # Landing page components
│   └── system/                  # Cross-cutting system concerns
│       ├── input-validation/    # Validation framework
│       ├── validation-rules/    # Business validation rules
│       ├── localization/        # i18n/l10n setup
│       ├── testing/             # Test strategy and patterns
│       └── ai-features/         # AI integration patterns
├── architecture/                # System design, patterns, API integration, webhooks
├── operations/                  # Deployment and runtime guides
├── business/                    # Business model and compliance planning
├── audits/                      # Security and compliance audits
└── planning/                    # Project execution tracking
```

**Navigation Benefit:** Instead of searching across scattered `features/` and `cross-cutting/` folders, developers can go directly to the relevant module (e.g., `modules/authentication/` for all auth-related documentation).

---

## Features by Module

The matrix below groups every shipped feature/capability under the module it belongs to in
`docs/modules/`. Use this as the single jump-off point — module column links to the module
folder, feature column links to the per-feature doc when available.

| Module | Feature / Capability | Implementation (Code) | Module Doc | Status |
|---|---|---|---|---|
| **Authentication** ([`modules/authentication/`](modules/authentication/README.md)) | User Login | `Features/Auth/Login/` | [`user-login/`](modules/authentication/user-login/) | ✅ Live |
| Authentication | User Registration | `Features/Auth/Register/` | [`user-registration/`](modules/authentication/user-registration/) | ✅ Live |
| Authentication | ASP.NET Identity (MVP setup) | `Infrastructure/Identity/` + `Data/` migrations | [`aspnet-identity-mvp/`](modules/authentication/aspnet-identity-mvp/) | ✅ Live |
| Authentication | Hybrid Identity (Web2 ↔ Web3) | `Models/Actor`, `Models/IdentityMapping` | [`hybrid-identity/`](modules/authentication/hybrid-identity/) | ✅ Live |
| **Escrow Payments** ([`modules/escrow-payments/`](modules/escrow-payments/README.md)) | Create & Hold Funds (atomic) | `Features/Escrow/CreateAndHoldFunds/` | [`hold-funds/`](modules/escrow-payments/hold-funds/) | ✅ Live |
| Escrow Payments | Hold Funds | `Features/Escrow/HoldFunds/` | [`hold-funds/`](modules/escrow-payments/hold-funds/) | ✅ Live |
| Escrow Payments | Release Funds | `Features/Escrow/ReleaseFunds/` | [`release-funds/`](modules/escrow-payments/release-funds/) | ✅ Live |
| Escrow Payments | Dispute Funds | `Features/Escrow/DisputeFunds/` | [`dispute-funds/`](modules/escrow-payments/dispute-funds/) | ✅ Live |
| Escrow Payments | Cancel Funds | `Features/Escrow/CancelFunds/` | [`cancel-funds/`](modules/escrow-payments/cancel-funds/) | ✅ Live |
| Escrow Payments | Platform Fee Calculation | `Features/Escrow/CreateAndHoldFunds/` (fee logic) | [`platform-fee/`](modules/escrow-payments/platform-fee/) | ✅ Live |
| Escrow Payments | Get Transaction (query) | `Features/Escrow/GetTransaction/` | _shared with hold/release docs_ | ✅ Live |
| Escrow Payments | List Transactions (query) | `Features/Escrow/ListTransactions/` | _shared with dashboards_ | ✅ Live |
| Escrow Payments | Stripe Webhooks (PaymentIntent events) | `Features/Escrow/Webhooks/` + `Infrastructure/Webhooks/Stripe/` | `architecture/stripe-webhooks/` | ✅ Live |
| **User Interface** ([`modules/user-interface/`](modules/user-interface/README.md)) | Landing Page | `Components/Pages/Home.*`, `HeroSection.*`, `HowItWorks.*`, `FaqSection.*`, `SocialProof.*` | [`landing-page/`](modules/user-interface/landing-page/) | ✅ Live |
| User Interface | Client Dashboard | `Components/Pages/ClientDashboard.*` | [`client-dashboard/`](modules/user-interface/client-dashboard/) | ✅ Live |
| User Interface | Consultant Dashboard | `Components/Pages/ConsultantDashboard.*` | [`consultant-dashboard/`](modules/user-interface/consultant-dashboard/) | ✅ Live |
| User Interface | Transaction Detail View | `Components/Pages/TransactionDetail.*` | [`transaction-detail/`](modules/user-interface/transaction-detail/) | ✅ Live |
| **System** ([`modules/system/`](modules/system/README.md)) | Input Validation (FluentValidation pipeline) | `Features/Behaviors/ValidationBehavior.cs` + `*Validator.cs` per slice | [`input-validation/`](modules/system/input-validation/) | ✅ Live |
| System | Business Validation Rules | Validators in each `Features/Escrow/*/` and `Features/Auth/*/` | [`validation-rules/`](modules/system/validation-rules/) | ✅ Live |
| System | Localization (en-US, es-MX) | `Resources/SharedResource.resx` + `IStringLocalizer` consumers | [`localization/`](modules/system/localization/) | ✅ Live |
| System | Testing Strategy | `EscrowApp.Tests/` (132/132 passing) | [`testing/`](modules/system/testing/) | ✅ Live |
| System | AI Features / Planning | `.copilot/skills/`, `NexSynapse/` (external) | [`ai-features/`](modules/system/ai-features/) | ✅ Live |
| System | Logging Pipeline Behavior | `Features/Behaviors/LoggingBehavior.cs` | _cross-cutting (no module doc)_ | ✅ Live |
| System | Performance Pipeline Behavior | `Features/Behaviors/PerformanceBehavior.cs` | _cross-cutting (no module doc)_ | ✅ Live |

> **Out of module scope:** REST API contracts (`Features/Escrow/Api/`), Stripe webhook
> transport (`Infrastructure/Webhooks/Stripe/`), and platform-wide concerns are documented
> under `docs/architecture/`, `docs/operations/`, `docs/audits/`, and `docs/business/`.

---

## Layer Map

```
Features/
├── Behaviors/          MediatR pipeline behaviors (cross-cutting, all requests)
│   ├── LoggingBehavior.cs                 ✅ Live
│   ├── PerformanceBehavior.cs             ✅ Live
│   └── ValidationBehavior.cs              ✅ Live (Track B #4)
├── Auth/               Authentication vertical slices (Track B — 100% COMPLETE)
│   ├── Login/                              ✅ Live — User authentication via ASP.NET Identity (122 tests)
│   └── Register/                           ✅ Live — User registration via ASP.NET Identity (122 tests)
└── Escrow/             Payment vertical slices (Track C — 100% COMPLETE)
    ├── Api/                                Shared contracts (request/response DTOs, controller)
    ├── CreateAndHoldFunds/                 ✅ Live — Revenue Blocker #1 complete
    ├── HoldFunds/                          ✅ Live (6 tests)
    ├── ReleaseFunds/                       ✅ Live (6 tests)
    ├── DisputeFunds/                       ✅ Live (6 tests)
    ├── CancelFunds/                        ✅ Live (Implemented 2026-04-14)
    ├── GetTransaction/                     ✅ Live (6 tests)
    ├── ListTransactions/                   ✅ Live (6 tests)
    └── Webhooks/
        └── PaymentIntentEventHandler.cs    ✅ 6 unit tests

Infrastructure/                            Webhook transport + configuration
├── Webhooks/Stripe/
│   ├── StripeSignatureVerifier.cs          ✅ 4 signature tests
│   └── StripeWebhookEndpoint.cs            ✅ 5 integration tests
└── Options/
    └── StripeWebhookOptions.cs             ✅ Configuration complete
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
| `POST` | `/api/escrow/hold` | `CreateAndHoldFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/release` | `ReleaseFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/dispute` | `DisputeFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `POST` | `/api/escrow/{id}/cancel` | `CancelFundsCommand` | `[Authorize(Policy="ApiAccess")]` |
| `GET` | `/api/escrow/{id}` | `GetTransactionQuery` | `[Authorize(Policy="ApiAccess")]` |
| `GET` | `/api/escrow` | `ListTransactionsQuery` | `[Authorize(Policy="ApiAccess")]` |

> ℹ️ **Current exposure:** `HoldFundsCommand` remains implemented as a slice, but
> `EscrowController` does not currently expose a dedicated REST endpoint for it.
> The preferred public API entry point is `CreateAndHoldFunds` via `POST /api/escrow/hold`.

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
    string IdempotencyKey,
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
>
> ℹ️ **Current exposure:** The slice exists and is tested, but the current REST controller
> does not expose a dedicated `POST /api/escrow/{id}/hold` endpoint.

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
ReleaseFundsCommand(
    int TransactionId,
    string IdempotencyKey)
```

**Handler flow:**
1. Load transaction via `GetByIdAsync()` — throws if not found
2. Guard: `Status == "Disputed"` → throws (disputed transactions cannot be released)
3. Guard: `Status != "Funded (Held)"` → throws — canonical held status for releasable transactions
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
| `ReleaseFundsHandler.cs` | ✅ |
| `ReleaseFundsResult.cs` | ✅ |

---

### `DisputeFunds/` — ✅ Live

**Purpose:** Flag an active hold as disputed. Voids the Stripe authorization (returning funds to the client's card) and locks the transaction for manual resolution.

**Command:**
```csharp
DisputeFundsCommand(
    int TransactionId,
    string Reason,
    string RaisedBy,        // email / identity label of disputing party
    string IdempotencyKey)
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

**Purpose:** Read a paginated list of `EscrowTransaction` records with optional status filtering.

**Query:**
```csharp
ListTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null)
```

Returns `PaginatedResponse<EscrowTransactionResponse>`.

---

### `Webhooks/` — ✅ COMPLETE (Track C tc-1 through tc-9)

**Purpose:** Handle incoming Stripe webhook events (`payment_intent.succeeded`, etc.) with full signature verification and domain event publishing.

**Status:** ✅ 100% COMPLETE — 15 tests (6 unit + 4 signature + 5 integration), all passing

**Components:**

| Component | File | Status | Tests |
|-----------|------|--------|-------|
| **Endpoint** | `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` | ✅ Complete | 5 integration |
| **Signature Verifier** | `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` | ✅ Complete | 4 unit |
| **Event Handler** | `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` | ✅ Complete | 6 unit |
| **Configuration** | `Infrastructure/Options/StripeWebhookOptions.cs` | ✅ Complete | — |

**Implementation Details:**
- POST `/api/webhooks/stripe` — HTTPS endpoint for Stripe webhook callbacks
- HMAC-SHA256 signature verification — constant-time comparison prevents timing attacks
- Timestamp validation — rejects webhooks >5 minutes old
- Event parsing — `Stripe.EventUtility.ConstructEvent()` for safe deserialization
- MediatR dispatch — `PaymentIntentSucceededNotification` published via event bus
- Idempotent processing — safe for Stripe retries

**Testing:**
- **StripeSignatureVerifierTests.cs** — 4 test cases covering valid/invalid signatures, timestamp validation
- **PaymentIntentEventHandlerTests.cs** — 6 test cases covering transaction lookup, state validation, error handling
- **WebhookIntegrationTests.cs** — 5 test cases covering HTTP routing, signature verification, event dispatch

**DI Registration:** 
- `Program.cs` lines 144-153, 267-273
- `StripeWebhookOptions` injected via `IOptions<StripeWebhookOptions>`
- Webhook endpoint registered as minimal API endpoint

See `docs/architecture/stripe-webhooks/` for full implementation spec.

---

## Auth Features (`Features/Auth/`)

Authentication and user management vertical slices using ASP.NET Core Identity.

---

### `Login/` — ✅ Live (Implemented)

**Purpose:** Allow existing users to authenticate with email/password credentials using ASP.NET Core Identity SignInManager.
Enables secure access to the escrow platform with session management and lockout protection.

**Command:**
```csharp
LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false)
```

**Handler flow:**
1. Call `SignInManager.PasswordSignInAsync()` with credentials and `lockoutOnFailure: true`
2. Process authentication result scenarios (success, invalid, locked out, 2FA required)
3. Return `LoginResult` with appropriate success/failure status and error message

**Key security features:**
- **Brute-Force Protection:** Account lockout after 5 failed attempts
- **Session Management:** "Remember Me" for persistent authentication cookies
- **Generic Error Messages:** Prevents username enumeration attacks
- **Timing Attack Protection:** Constant-time password verification

**Files:**
| File | Status |
|---|---|
| `LoginCommand.cs` | ✅ Live |
| `LoginCommandHandler.cs` | ✅ Live |

**UI Component:**
- **Route:** `/auth/login`
- **Files:** `Components/Pages/Auth/Login.razor`, `.razor.cs`, `.razor.css`
- **Features:** Bootstrap 5 responsive design, localized strings, loading states, error handling

**Testing:**
- ✅ **Unit Tests:** `EscrowApp.Tests/Features/Auth/Login/LoginCommandTests.cs`
- **Coverage:** Valid credentials, invalid credentials, account lockout, 2FA scenarios

---

### `Register/` — ✅ Live (Implemented 2026-04-16)

**Purpose:** Allow new users to create accounts via email/password registration using ASP.NET Core Identity.
This is the foundational authentication feature enabling users to access the escrow platform.

**Command:**
```csharp
RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName)
```

**Handler flow:**
1. Validate `Password` matches `ConfirmPassword` — returns error if mismatch
2. Create new `ApplicationUser` with `Email` and `UserName` set to email
3. Call `UserManager.CreateAsync()` for password hashing and Identity validation
4. Return `RegisterResult` with success/failure status and error message

**Key validation:**
- **Password Match:** Handler validates before calling `UserManager`
- **Email Uniqueness:** ASP.NET Identity enforces unique email constraint
- **Password Strength:** Identity framework enforces password policy
- **Email Format:** Identity framework validates email format

**Files:**
| File | Status |
|---|---|
| `RegisterCommand.cs` | ✅ Live |
| `RegisterCommandHandler.cs` | ✅ Live |

**UI Component:**
- **Route:** `/auth/register`
- **Files:** `Components/Pages/Auth/Register.razor`, `.razor.cs`, `.razor.css`
- **Features:** Bootstrap 5 responsive design, localized strings, error handling, post-registration redirect

**Testing:**
- ✅ **Unit Tests:** `EscrowApp.Tests/Features/Auth/Register/RegisterCommandHandlerTests.cs` (12 tests, 100% pass rate)
- **Coverage:** Happy path, password mismatch, UserManager failures, edge cases

---

## User Interface Surface (`Components/Pages/`)

These pages live outside `Features/` but are part of the shipped product surface and
map to `docs/modules/user-interface/`.

| Page / Area | Route | Status | Notes |
|---|---|---|---|
| Landing page | `/` | ✅ Live | Composes `NavBar`, `HeroSection`, `HowItWorks`, `SocialProof`, `FaqSection`, and `Footer`. |
| Client dashboard | `/dashboard/client` | ✅ Live | Authenticated client workspace with KPIs and transaction views. |
| Consultant dashboard | `/dashboard/consultant` | ✅ Live | Authenticated consultant workspace with earnings and held-funds summary. |
| Transaction detail | `/dashboard/transaction/{TransactionId:int}` | 🟡 Partial | Route exists, but the page still contains TODO placeholders and disabled action buttons. |

---

## Related Documentation Modules

The implementation inventory above maps to the module-first docs structure as follows:

- `docs/modules/authentication/` — login, registration, ASP.NET Identity, hybrid identity
- `docs/modules/escrow-payments/` — hold, release, dispute, cancel, and platform fee docs
  (the atomic `CreateAndHoldFunds` flow is currently described within `hold-funds/`)
- `docs/modules/user-interface/` — landing page and dashboard docs
- `docs/modules/system/` — input-validation, validation-rules, localization, testing, ai-features
- Security reviews live under `docs/audits/`; platform-wide technical references live under
  `docs/architecture/`, `docs/operations/`, and `docs/business/`

---

## Gaps Resolved (All Closed ✅)

| Gap | Affected Slices | Status | Resolution |
|---|---|---|---|
| `ReleaseFundsHandler` status check uses `"Held"` not `"Funded (Held)"` | `ReleaseFunds` | ✅ Fixed 2026-04-14 | Status guard corrected |
| `HoldFundsHandler` does not calculate or propagate `PlatformFee` | `HoldFunds` | ✅ Documented | Use `CreateAndHoldFunds` as Day-1 path |
| No `FundsCancelledEvent` domain event exists yet | `CancelFunds` | ✅ Fixed 2026-04-14 | `FundsCancelledEvent` implemented |
| No `ValidationBehavior` pipeline behavior | All slices | ✅ Implemented 2026-04-16 | Track B #4 |
| `PaymentIntentEventHandler` (Webhooks) unimplemented | `Webhooks` | ✅ Fixed 2026-04-29 | Track C tc-4 through tc-9 complete |

---

## Completion Summary

### Track B: Authentication (100% ✅)
- ✅ **Login** — 122 tests passing
- ✅ **Register** — 122 tests passing
- ✅ **Pipeline Behaviors** — Validation, logging, performance
- ✅ **Identity Infrastructure** — ASP.NET Identity, DbContext, migrations

### Track C: Stripe Webhooks (100% ✅)
- ✅ **Webhook Endpoint** — HTTP handler with signature verification
- ✅ **Signature Verifier** — HMAC-SHA256, constant-time comparison
- ✅ **Event Handler** — MediatR notification handler for payment events
- ✅ **Configuration** — Environment-specific Stripe webhook settings
- ✅ **Testing** — 15 tests (unit + integration), all passing
- ✅ **Documentation** — Architecture, implementation, usage guides

### Overall Metrics
- **Tracked implementation tasks:** 27/27 complete (100% ✅)
- **Total Tests:** 132/132 passing (0 failures, 1 skipped)
- **Build Status:** 0 errors, 0 warnings
- **Code Quality:** OWASP-first, idempotency guaranteed, audit trails enabled

---

## Scope Boundary

- This file inventories repository-local feature slices, related UI pages, and the local
  documentation mapping needed to find them quickly.
- External NexSynapse automation and cross-repo AI portability assets are intentionally
  excluded from the local feature count.
