# Escrow Prototype — Execution Checklist

> Last synced with codebase: 2026-04-13
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

- [ ] **#1 — Platform fee (1.5%)** ⚠️ REVENUE BLOCKER
  - [ ] Add `PlatformFee` (decimal) + `PlatformFeePercentage` (decimal) fields to `EscrowTransaction`
  - [ ] Create EF Core migration for new columns
  - [ ] Add `Platform:FeePercentage` config to `appsettings.json` (default: `0.015`)
  - [ ] Implement fee calculation in `CreateAndHoldFundsHandler` (`amount × rate`)
  - [ ] Include platform fee in Stripe charge amount (`escrowAmount + platformFee`)
  - [ ] Publish fee amount in domain events for audit trail
- [ ] **#2 — CancelFunds handler**
  - [ ] Replace `NotImplementedException` with real logic
  - [ ] Cancel Stripe PaymentIntent via `IFundCancellable`
  - [ ] Update transaction status to `Cancelled`
  - [ ] Publish `FundsCancelledEvent` via `IEventBus`
- [ ] **#4 — FluentValidation on all commands**
  - [ ] `CreateAndHoldFundsCommandValidator` (amount > 0, emails required, idempotency key)
  - [ ] `HoldFundsCommandValidator`
  - [ ] `ReleaseFundsCommandValidator`
  - [ ] `DisputeFundsCommandValidator` (reason required)
  - [ ] `CancelFundsCommandValidator`
  - [ ] Register validation pipeline behavior in `Program.cs`
- [ ] **#5 — Real unit tests (replace 16 stubs)**
  - [ ] `HoldFundsHandlerTests` — 3 real tests with Moq + FluentAssertions
  - [ ] `ReleaseFundsHandlerTests` — 3 real tests
  - [ ] `DisputeFundsHandlerTests` — 2 real tests
  - [ ] `CancelFundsHandlerTests` — 4 real tests
  - [ ] `StripePaymentStrategyTests` — 4 real tests with mocked Stripe SDK
- [x] **#6 — Production secrets** *(partially done — 2026-04-11 security audit)*
  - [x] Remove hardcoded `sk_test_MockEscrowAPIKey123` from `appsettings.json`
  - [x] Remove hardcoded DB connection string with `Password=admin123`
  - [x] Remove hardcoded API key from `appsettings.Development.json`
  - [x] Create `appsettings.Production.json` template with placeholder comments
  - [x] Document required environment variables in deployment doc

### Track B: User Access (parallel with Track A)

- [ ] **#3 — User authentication (ASP.NET Identity)**
  - [ ] Add ASP.NET Identity NuGet packages
  - [ ] Configure Identity in `Program.cs`
  - [ ] Create `ApplicationUser` entity + Identity DbContext integration
  - [ ] Wire Login page backend (`Login.razor.cs`)
  - [ ] Wire Register page backend (`Register.razor.cs`)
  - [ ] Add `[Authorize]` on all business pages (dashboards, transaction detail)
  - [ ] Session management for Blazor Server

### Track C: Stripe Sync (parallel after #1)

- [ ] **#7 — Minimal Stripe webhook**
  - [ ] Register webhook endpoint in `Program.cs`
  - [ ] Implement Stripe signature verification (`StripeWebhookEndpoint`)
  - [ ] Handle `payment_intent.succeeded` event → update transaction status
  - [ ] Return 200 OK for unhandled event types (don't break Stripe retry)

### Merge Point

- [ ] **#8 — Cloud deployment** (requires #3 + #6)
  - [ ] Choose hosting: Azure App Service or Render
  - [ ] Configure environment variables for Stripe key + DB connection
  - [ ] Deploy using existing Dockerfile
  - [ ] Verify HTTPS + HSTS enforcement
  - [ ] Smoke test: create → hold → release flow with Stripe test card

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
