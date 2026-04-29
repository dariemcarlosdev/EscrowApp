# Features — Vertical Slice Inventory

> Last synced with codebase: 2026-04-16 (Reorganized: Documentation restructured by module/concern)
> Layer: **Application** — `Features/Escrow/` + `Features/Auth/` (MediatR CQRS vertical slices)

This document is the ground-truth inventory of every vertical slice and pipeline behavior
in the `Features/` folder. It maps each slice to its implementation status, command/result
contracts, and the domain operations it owns.

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
│   │   ├── hold-funds/          # Hold funds flow documentation
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
│       ├── ai-features/         # AI integration patterns
│       ├── portable-ai-sync/    # Bidirectional .copilot/.claude sync rule
│       └── security-compliance/ # OWASP Top 10 compliance framework
├── platform/                   # Platform architecture and operations
├── audits/                      # Security and compliance audits
└── planning/                    # Project execution tracking
```

**Navigation Benefit:** Instead of searching across scattered `features/` and `cross-cutting/` folders, developers can go directly to the relevant module (e.g., `modules/authentication/` for all auth-related documentation).

---

## Layer Map

```
Features/
├── Behaviors/          MediatR pipeline behaviors (cross-cutting, all requests)
│   ├── LoggingBehavior.cs        ✅ Live
│   ├── PerformanceBehavior.cs    ✅ Live
│   └── ValidationBehavior.cs     ✅ Live (Track A #4)
├── Auth/               Authentication vertical slices
│   ├── Login/                ✅ Live — User authentication via ASP.NET Identity
│   └── Register/             ✅ Live — User registration via ASP.NET Identity
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
See `docs/platform/architecture/stripe-webhooks/` for the implementation spec.

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

## Cross-Cutting Gaps (Pre-Existing, Pre-Platform Fee)

| Gap | Affected Slices | Status |
|---|---|---|
| `ReleaseFundsHandler` status check uses `"Held"` not `"Funded (Held)"` | `ReleaseFunds` | ✅ Fixed 2026-04-14 |
| `HoldFundsHandler` does not calculate or propagate `PlatformFee` | `HoldFunds` | 🟡 Inconsistency — use `CreateAndHoldFunds` as Day-1 path |
| No `FundsCancelledEvent` domain event exists yet | `CancelFunds` | ✅ Fixed 2026-04-14 |
| No `ValidationBehavior` pipeline behavior | All slices | ✅ Implemented 2026-04-16 (Track A #4) |
| `PaymentIntentEventHandler` (Webhooks) unimplemented | `Webhooks` | 🟡 Track C #7 |

---

## Infrastructure Features — AI & Security

### Portable AI Architecture Sync — ✅ Live (Implemented 2026-04-16)

**Purpose:** Enforces bidirectional portability between `.copilot` and `.claude` configurations to maintain our core portability principle. Any change to AI infrastructure in one platform automatically syncs to the other.

**Rule Statement:** *When implementing rules, hooks, extensions, workflows, or instructions in `.copilot`, they MUST be translated and applied to `.claude`, and vice versa.*

**Components:**

| Component | File | Status |
|-----------|------|--------|
| **Rule Specification** | `PORTABLE-AI-SYNC-RULE.md` | ✅ Complete specification |
| **Bash Sync Script** | `scripts/ai-config-sync.sh` | ✅ Full bidirectional sync |
| **PowerShell Sync Script** | `scripts/ai-config-sync.ps1` | ✅ Windows-native equivalent |
| **Pre-commit Hook** | `.git/hooks/pre-commit` | ✅ Extended existing security hook |
| **GitHub Actions** | `.github/workflows/ai-config-sync.yml` | ✅ CI validation on PRs |
| **Implementation Guide** | `docs/ai-sync-implementation-guide.md` | ✅ Usage and troubleshooting |

**Translation Patterns:**

| Source Format | Target Format | Auto-Translated |
|---------------|---------------|-----------------|
| `.copilot/hooks/*.sh` | `.claude/hooks/*.ps1` | ✅ Bash ↔ PowerShell |
| `.copilot/copilot.yml` | `.claude/settings.json` | 🟡 Planned |
| `.copilot/extensions/` | `.claude/rules/` | 🟡 Planned |
| Skills (universal) | `.github/skills/` → bridges | ✅ Bridge pattern |

**Enforcement:**
- ✅ **Pre-commit:** Blocks commits violating sync rule
- ✅ **CI/CD:** GitHub Actions validates all PRs
- ✅ **Scripts:** Manual and automatic sync validation
- ✅ **Documentation:** Complete rule specification and guides

**Usage:**
```bash
# Validate compliance
./scripts/ai-config-sync.sh --validate --strict

# Perform sync  
./scripts/ai-config-sync.sh --sync

# PowerShell (Windows)
.\scripts\ai-config-sync.ps1 -Sync -Validate
```

**Benefits:**
- 🚀 **Automated Enforcement:** Pre-commit hooks prevent violations
- 🔄 **Bidirectional Sync:** Changes automatically translate both ways
- 📋 **Platform Agnostic:** Same workflows on Copilot CLI and Claude Code
- ✅ **Validated:** CI ensures sync compliance on all PRs

---
