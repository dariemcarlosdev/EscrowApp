# Escrow Prototype — Execution Checklist

> Last synced with codebase: 2026-04-16 01:21
> Scope method: **mvp-gatekeeper** revenue gate applied to every item

---

## Foundation — ✅ COMPLETE

### Phase 1: Landing Page ✅

- [x] Initialize project directory and repository
- [x] Corporate visual design — Bootstrap 5 fintech theme with glassmorphism
- [x] Hero Section component (`Components/Shared/HeroSection.*`)
- [x] "How It Works" section (`Components/Shared/HowItWorks.*`)
- [x] FAQ accordion (`Components/Shared/FaqSection.*`)
- [x] Social proof / trust section (`Components/Shared/SocialProof.*`)
- [x] NavBar + Footer with localization (`Components/Shared/NavBar.*`, `Footer.*`)
- [x] Home page orchestrator (`Components/Pages/Home.*`)

### Phase 2: Escrow Engine — Scaffolding ✅

- [x] .NET 10 Blazor Server solution with Clean Architecture layers
- [x] Domain models: `EscrowTransaction`, `Actor`, `IdentityMapping`
- [x] EF Core + PostgreSQL (`EscrowDbContext`, Npgsql)
- [x] Repository pattern (`IEscrowTransactionRepository` + implementation)
- [x] 3 EF migrations: InitialCreate → HybridIdentity → DisputeFundsSlice
- [x] Strategy pattern: `IFundHoldable`, `IFundReleasable`, `IFundCancellable` (ISP)
- [x] `StripePaymentStrategy` — full implementation (hold, release, cancel)
- [x] `IPaymentStrategyFactory` — OCP-compliant provider resolution
- [x] Idempotency keys on all payment operations
- [x] Manual capture flow (authorize → hold → capture on release)
- [x] `CreateAndHoldFunds` handler — create + hold atomically
- [x] `HoldFunds` handler — hold on existing transaction
- [x] `ReleaseFunds` handler — capture held PaymentIntent
- [x] `DisputeFunds` handler — cancel hold + flag as disputed
- [x] `GetTransaction` / `ListTransactions` — read queries
- [x] `DomainEvent` base + `PaymentReceivedEvent` + `DisputeRaisedEvent`
- [x] `InMemoryEventBus` — MVP implementation
- [x] `EscrowController` — 6 REST endpoints
- [x] API key auth (`ApiKeyAuthenticationHandler`)
- [x] `ApiExceptionMiddleware` — RFC 7807 ProblemDetails
- [x] Swagger/OpenAPI with API key security (dev only)
- [x] Policy-based authorization (`ApiAccess`)
- [x] `Program.cs` — all services registered

### Phase 3: Web UI — Scaffolding ✅

- [x] Client Dashboard (`/dashboard/client`)
- [x] Consultant Dashboard (`/dashboard/consultant`)
- [x] Transaction Detail (`/transaction/{id}`)
- [x] Login page (`/auth/login`) — UI shell
- [x] Register page (`/auth/register`) — UI shell
- [x] Error + NotFound pages
- [x] `IStringLocalizer<SharedResource>` wired + .resx files (en-US, es-MX)
- [x] Culture switch endpoint (`/culture/set`)
- [x] Dockerfile + docker-compose.yml
- [x] CI/CD pipeline (`.github/workflows/ci.yml`)

---

## 🚀 MVP Release — Ship-to-Charge

> **Revenue gate:** Every item below directly enables or protects Day-1 revenue.

### Track A: Money Pipe (sequential)

- [x] **#1 — Platform fee (1.5%)** ✅ DONE — 2026-04-14
  - [x] Add `PlatformFee` (decimal) + `PlatformFeePercentage` (decimal) fields to `EscrowTransaction`
  - [x] Create EF Core migration (`AddPlatformFeeToEscrowTransaction`)
  - [x] Add `Platform:FeePercentage` config to `appsettings.json` + `appsettings.Production.json` (default: `0.015`)
  - [x] `Infrastructure/Options/PlatformOptions.cs` — typed Options record registered in `Program.cs`
  - [x] Implement fee calculation in `CreateAndHoldFundsHandler` (`max(amount × rate, minimumFee)`)
  - [x] Include platform fee in Stripe charge amount (`escrowAmount + platformFee`)
  - [x] Publish `PlatformFee` + `PlatformFeePercentage` in `PaymentReceivedEvent` for audit trail
  - [x] Extend `EscrowTransactionResponse` with `PlatformFee`, `PlatformFeePercentage`, `TotalCharged`
- [x] **#2 — CancelFunds handler** ✅ DONE — 2026-04-14
  - [x] Replace `NotImplementedException` with real logic
  - [x] Cancel Stripe PaymentIntent via `IFundCancellable`
  - [x] Update transaction status to `Cancelled`
  - [x] Publish `FundsCancelledEvent` via `IEventBus`
- [x] **#4 — FluentValidation on all commands** ✅ DONE — 2026-04-16
  - [x] `CreateAndHoldFundsCommandValidator` (amount > 0, emails required, idempotency key)
  - [x] `HoldFundsCommandValidator`
  - [x] `ReleaseFundsCommandValidator`
  - [x] `DisputeFundsCommandValidator` (reason required)
  - [x] `CancelFundsCommandValidator`
  - [x] Register validation pipeline behavior in `Program.cs`
- [x] **#5 — Real unit tests (replace 16 stubs)** ✅ DONE — 2026-04-16
  - [x] `HoldFundsHandlerTests` — 3 real tests with Moq + FluentAssertions
  - [x] `ReleaseFundsHandlerTests` — 3 real tests
  - [x] `DisputeFundsHandlerTests` — 2 real tests
  - [x] `CancelFundsHandlerTests` — 4 real tests
  - [x] `StripePaymentStrategyTests` — 5 real tests with mocked Stripe SDK
  - [x] All 51 tests passing (validators + handlers + strategy)
- [x] **#6 — Production secrets** ✅ DONE — 2026-04-16 (security audit 2026-04-11)
  - [x] Remove hardcoded `sk_test_MockEscrowAPIKey123` from `appsettings.json`
  - [x] Remove hardcoded DB connection string with `Password=admin123`
  - [x] Remove hardcoded API key from `appsettings.Development.json`
  - [x] Create `appsettings.Production.json` template with placeholder comments
  - [x] Document required environment variables in deployment doc

### Track B: User Access (parallel with Track A)

- [ ] **#3 — User authentication (ASP.NET Identity)** — **4 of 14 slices complete (29%)**
  - [x] Install NuGet packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` ✅ Slice 2
  - [x] Create `Models/ApplicationUser.cs` (extends IdentityUser<int>, links to Actor) ✅ Slice 1
  - [x] Add Identity configuration to `EscrowDbContext` + migration ✅ Slices 2-3
  - [x] Register Identity in `Program.cs` (AddIdentity, AddAuthentication, AddAuthorization) ✅ Slice 4
  - [ ] Configure Blazor auth: `RevalidatingServerAuthenticationStateProvider` + `<CascadingAuthenticationState>` ⏳ Slice 5
  - [ ] Create `Components/Pages/Login.razor` + `Login.razor.cs` (code-behind pattern) ⏳ Slice 6
  - [ ] Create `Components/Pages/Register.razor` + `Register.razor.cs` (code-behind pattern) ⏳ Slice 7
  - [ ] Add `[Authorize]` attribute on dashboard pages ⏳ Slice 10
  - [ ] Implement logout button in `NavBar.razor.cs` ⏳ Slice 8
  - [ ] Add login/register localization keys to `Resources/SharedResource.resx` ⏳ Slice 9
  - [ ] Unit test: RegisterHandlerTests (create user, hash password, actor linkage) ⏳ Slice 11
  - [ ] Unit test: LoginHandlerTests (valid/invalid credentials, session creation) ⏳ Slice 11
  - [ ] Integration test: Login flow (register → login → redirect to dashboard) ⏳ Slice 12
  - [ ] Document: `docs/cross-cutting/authentication/aspnet-identity-mvp.md` ✅ **Done**

### Track C: Stripe Sync (parallel after #1)

- [ ] **#7 — Minimal Stripe webhook**
  - [ ] Create `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` (POST /api/webhooks/stripe)
  - [ ] Create `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` (HMAC-SHA256 validation)
  - [ ] Create `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` (INotificationHandler)
  - [ ] Register verifier + endpoint in `Program.cs`
  - [ ] Configure webhook secret in `appsettings.json` (use env var override)
  - [ ] Test signature verification (valid, invalid, old timestamp cases)
  - [ ] Test event handler (payment_intent.succeeded only, other events ignored)
  - [ ] Local test with Stripe CLI: `stripe listen --forward-to localhost:8080/api/webhooks/stripe`
  - [ ] Document: `docs/architecture/stripe-webhooks/minimal-webhook-handler-mvp.md` ✅ **Done**
  - [ ] All webhook tests passing (signature + event handler)

### Merge Point

- [ ] **#8 — Cloud deployment** (requires #3 + #6) ✅ **Docs Ready**
  - [ ] Create Azure resource group + container registry
  - [ ] Build & push Docker image to Azure Container Registry (ACR)
  - [ ] Create PostgreSQL Flexible Server (managed database)
  - [ ] Create Azure Key Vault (secret storage)
  - [ ] Create managed identity (no hardcoded secrets)
  - [ ] Create Container Apps environment + deploy app
  - [ ] Apply database migrations (dotnet ef database update)
  - [ ] Configure Stripe webhooks endpoint in Stripe dashboard
  - [ ] Health check endpoint: GET /health → 200 OK
  - [ ] Smoke test: Register → Login → Create transaction
  - [ ] Smoke test: Webhook signature verification
  - [ ] Setup Application Insights monitoring
  - [ ] Configure alerting (restart rate, DB pool exhaustion, 5xx errors)
  - [ ] Document: `docs/operations/deployment/cloud-deployment-steps-mvp.md` ✅ **Done**
  - [ ] Create rollback procedure (revert to previous image version)

---

## 📊 Business Strategy & AI Infrastructure — ✅ COMPLETE

### Business Strategy

- [x] Competitive analysis vs Stripe DIY, Escrow.com, Upwork/Fiverr, PayPal, Tazapay/Payoneer
- [x] Business model risk factors upgraded with severity ratings
- [x] Strategic plan with 4 pre-launch blockers (`docs/business/business-model/strategic-plan.md`)
- [x] Regulatory compliance rules added to all 4 instruction files (AGENTS.md, CLAUDE.md, GEMINI.md, copilot-instructions.md)
- [x] Project framing updated: "escrow platform" → "escrow-like secure payment holding"

### AI Infrastructure Export System

- [x] AI Infrastructure Export Guide (`.github/AI-INFRASTRUCTURE-EXPORT-GUIDE.md`)
- [x] Export starter kit script (`.github/scripts/export-ai-infrastructure.ps1`)
- [x] Project tailoring wizard (`.github/scripts/tailor-ai-infrastructure.ps1`)

### AI Compatibility Audit (4-Tool)

- [x] GitHub Copilot CLI audit — ✅ 100% compatible
- [x] Claude Code audit — ✅ 100% compatible
- [x] Google Gemini/Antigravity audit — ✅ 100% compatible (added `.agent/rules/`, `.agent/workflows/`, `.gemini/settings.json`)
- [x] OpenAI Codex CLI audit — ✅ 100% (added subdirectory `AGENTS.md` files, `config.toml` `[context]` section)
- [x] Created `CODEX.md` (Codex CLI-specific instructions, 200 lines)
- [x] Created `.codex/config.toml` + `.codex/README.md`
- [x] Created `.claudeignore` (token optimization)
- [x] Fixed skill count drift across 5 files (36/41 → 43)
- [x] Updated AGENTS.md categories table to match CATALOG.md v2.3.0
- [x] Consolidated audit report (`.github/AI-COMPATIBILITY-AUDIT.md`)

---

## 📋 Post-MVP Backlog — Explicitly Deferred

> Each item has an **upgrade trigger**. Do NOT build until the trigger fires.

- [ ] Email notifications on state transitions — _trigger: user retention < 60%_
- [ ] Polly resilience (retry, circuit breaker, bulkhead) — _trigger: tx volume > 100/day_
- [ ] Comprehensive test coverage (>90% on payment flows) — _trigger: next feature sprint_
- [ ] Web3/Ethereum bridge (wallet verification, smart contracts) — _trigger: 3+ user requests_
- [ ] PayPal payment strategy — _trigger: 5+ user requests for PayPal_
- [ ] Admin dashboard for escrow oversight — _trigger: admin ops > 10/week_
- [ ] .resx localization audit (completeness check) — _trigger: user reports missing translation_
- [ ] Real-time notifications (SignalR) — _trigger: 100+ concurrent users_
- [ ] Formal audit trail / transaction history log — _trigger: regulatory compliance required_
- [ ] Performance monitoring (health checks, telemetry) — _trigger: production traffic stable_
- [ ] UI polish (toasts, spinners, loading states) — _trigger: user feedback requests it_
- [ ] Multi-currency support — _trigger: international user demand proven_
- [ ] Express Payout ($5 or 1% fee) — _trigger: 50+ transactions/month_
- [ ] Dispute arbitration service ($25/case) — _trigger: 10+ disputes/month_
