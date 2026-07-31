# Escrow Prototype — Execution Checklist

> $12026-05-01 08:26 EDT (User Stories added to 16 module docs + AI features roadmap Overview)
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
  - [x] Configure Blazor auth: `RevalidatingServerAuthenticationStateProvider` + `<CascadingAuthenticationState>` /mode 
  - [x] Create `Components/Pages/Login.razor` + `Login.razor.cs` (code-behind pattern) ⏳ Slice 6
  - [x] Create `Components/Pages/Register.razor` + `Register.razor.cs` (code-behind pattern) ⏳ Slice 7
  - [x] Add `[Authorize]` attribute on dashboard pages ⏳ Slice 10
  - [x] Implement logout button in `NavBar.razor.cs` ⏳ Slice 8
  - [x] Add login/register localization keys to `Resources/SharedResource.resx` ⏳ Slice 9
  - [x] Unit test: RegisterHandlerTests (create user, hash password, actor linkage) ⏳ Slice 11
  - [x] Unit test: LoginHandlerTests (valid/invalid credentials, session creation) ⏳ Slice 11
  - [x] Integration test: Login flow (register → login → redirect to dashboard) ⏳ Slice 12
  - [x] Document: `docs/cross-cutting/authentication/aspnet-identity-mvp.md` ✅ **Done**

### Track C: Stripe Sync (parallel after #1)

- [x] **#7 — Minimal Stripe webhook**
  - [x] Create `Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` (POST /api/webhooks/stripe)
  - [x] Add Development-only `GET /api/webhooks/stripe` diagnostic response for manual browser checks
  - [x] Create `Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` (HMAC-SHA256 validation)
  - [x] Create `Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` (INotificationHandler)
  - [x] Register verifier + endpoint in `Program.cs`
  - [x] Configure webhook secret in `appsettings.json` (use env var override)
  - [x] Test signature verification (valid, invalid, old timestamp cases)
  - [x] Test event handler (payment_intent.succeeded only, other events ignored)
  - [ ] Local test with Stripe CLI: `stripe listen --forward-to http://localhost:5093/api/webhooks/stripe`
  - [x] Add manual test guide: `docs/Test/local-stripe-cli-webhook-test.md`
  - [x] Document: `docs/architecture/stripe-webhooks/minimal-webhook-handler-mvp.md` ✅ **Done**
  - [x] All webhook tests passing (signature + event handler)
  cl
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


---

## 📋 Post-MVP Backlog — Structured Deferred Work

> Canonical detail lives in [post-mvp/post-mvp-reference.md](post-mvp/post-mvp-reference.md), [v1.1-roadmap.md](post-mvp/v1.1-roadmap.md), [post-mvp-patterns-analysis.md](post-mvp/post-mvp-patterns-analysis.md), and [post-mvp-implementation-guide.md](post-mvp/post-mvp-implementation-guide.md).
> Checklist rule: `task-checklist.md` owns completion status; the `post-mvp/` docs own design depth, sequencing, and acceptance criteria.

### Track D: Advanced Webhook Reliability (v1.1)

- [ ] **tc-12 - Event Deduplication**
  - [ ] tc-12a - Create `Webhooks` table schema + migration
  - [ ] tc-12b - Update `StripeWebhookEndpoint` to short-circuit duplicate Stripe events
  - [ ] tc-12c - Implement `WebhookDeduplicationService`
  - [ ] tc-12d - Add automated coverage for valid, duplicate, expired, and malformed webhook events
  - [ ] tc-12e - Return `X-Webhook-Id` and `X-Duplicate` response headers for ops visibility
  - [ ] tc-12f - Create `docs\architecture\patterns\event-deduplication.md`

- [ ] **tc-13 - Event Sourcing + Transaction Timeline**
  - [ ] tc-13a - Create `PaymentEvents` table schema + migration
  - [ ] tc-13b - Add append-only `PaymentEventStore`
  - [ ] tc-13c - Compute transaction status from the latest event while preserving backward compatibility
  - [ ] tc-13d - Update `CreateAndHoldFundsHandler` to append payment events
  - [ ] tc-13e - Update `ReleaseFundsHandler` to append payment events
  - [ ] tc-13f - Update `DisputeFundsHandler` to append payment events
  - [ ] tc-13g - Add tests for append flow, audit trail retrieval, computed status, and crash recovery
  - [ ] tc-13h - Add `TransactionTimeline` dashboard UI
  - [ ] tc-13i - Create `docs\architecture\patterns\event-sourcing.md`

- [ ] **tc-14 - Outbox Pattern + Delivery Guarantees**
  - [ ] tc-14a - Create `OutboxEvents` table schema + migration
  - [ ] tc-14b - Add `OutboxPublishingService` background worker
  - [ ] tc-14c - Update payment handlers to persist outbox rows in the same transaction as domain changes
  - [ ] tc-14d - Add `/health/outbox` lag endpoint
  - [ ] tc-14e - Add integration coverage for crash, restart, and exactly-once delivery
  - [ ] tc-14f - Create `docs\architecture\patterns\outbox-pattern.md` and update `docs\architecture\event-bus\event-bus.md`

### Track E: Workflow Recovery & Operations (v1.2)

- [ ] **tc-15 - Saga Pattern**
  - [ ] Model long-running dispute and refund workflows with compensation logic
  - [ ] Persist saga state, retries, and timeout transitions
  - [ ] Add tracing and metrics for saga state changes
  - [ ] Create `docs\architecture\patterns\saga-pattern.md`

- [ ] **tc-16 - Dead Letter Queue**
  - [ ] Create durable storage for unprocessable webhook and domain events
  - [ ] Add replay / investigation workflow for pending, investigated, fixed, and discarded items
  - [ ] Create `docs\architecture\patterns\dead-letter-queue.md`

### Track F: Optimization & Provider Resilience (v1.3+)

- [ ] **tc-17 - Event Enrichment**
  - [ ] Publish enriched payment events with client, consultant, and service context
  - [ ] Reduce dashboard N+1 lookups by broadening downstream event payload contracts
  - [ ] Create `docs\architecture\patterns\event-enrichment.md`

- [ ] **tc-18 - Circuit Breaker**
  - [ ] Add provider failure protection / fail-fast behavior for external payment APIs
  - [ ] Expose provider health monitoring and degraded-mode signals
  - [ ] Create `docs\architecture\patterns\circuit-breaker.md`

### Additional deferred backlog (not yet decomposed in `planning\post-mvp\`)

> Each item keeps its original upgrade trigger until it gets its own detailed planning document.

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
