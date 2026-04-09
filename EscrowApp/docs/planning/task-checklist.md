# Escrow Prototype — Execution Checklist

> Last synced with codebase: 2026-04-09

## Phase 1: Landing Page ✅ COMPLETE (except deploy)

- [x] Initialize project directory and repository
- [x] Corporate visual design — Bootstrap 5 fintech theme with glassmorphism
- [x] Hero Section component (`Components/Shared/HeroSection.*`)
- [x] "How It Works" section (`Components/Shared/HowItWorks.*`)
- [x] FAQ accordion (`Components/Shared/FaqSection.*`)
- [x] Social proof / trust section (`Components/Shared/SocialProof.*`)
- [x] NavBar + Footer with localization (`Components/Shared/NavBar.*`, `Footer.*`)
- [x] Home page orchestrator (`Components/Pages/Home.*`)
- [ ] Deploy Landing Page to cloud hosting

## Phase 2: Escrow Engine ✅ MOSTLY COMPLETE

### Architecture & Data Modeling
- [x] .NET 10 Blazor Server solution with Clean Architecture layers
- [x] Domain models: `EscrowTransaction`, `Actor`, `IdentityMapping`
- [x] EF Core + PostgreSQL (`EscrowDbContext`, Npgsql)
- [x] Repository pattern (`IEscrowTransactionRepository` + implementation)
- [x] 3 EF migrations: InitialCreate → HybridIdentity → DisputeFundsSlice

### Payment Integration (Stripe)
- [x] Strategy pattern: `IFundHoldable`, `IFundReleasable`, `IFundCancellable` (ISP)
- [x] `StripePaymentStrategy` — full implementation (hold, release, cancel)
- [x] `IPaymentStrategyFactory` — OCP-compliant provider resolution
- [x] Idempotency keys on all payment operations
- [x] Manual capture flow (authorize → hold → capture on release)

### MediatR CQRS Vertical Slices
- [x] `CreateAndHoldFunds` — create transaction + hold funds atomically
- [x] `HoldFunds` — hold funds on existing transaction
- [x] `ReleaseFunds` — capture held PaymentIntent
- [x] `DisputeFunds` — cancel hold + flag as disputed
- [ ] `CancelFunds` — **STUB** (`throw NotImplementedException`)
- [x] `GetTransaction` — query single transaction by ID
- [x] `ListTransactions` — paginated list with status filter

### Domain Events
- [x] `DomainEvent` abstract base class
- [x] `PaymentReceivedEvent` — published after hold
- [x] `DisputeRaisedEvent` — published after dispute
- [x] `InMemoryEventBus` — MVP implementation (logs to console)

### REST API
- [x] `EscrowController` — 6 endpoints under `/api/escrow/*`
- [x] API key authentication (`ApiKeyAuthenticationHandler`)
- [x] `ApiExceptionMiddleware` — RFC 7807 ProblemDetails
- [x] Swagger/OpenAPI with API key security scheme (dev only)
- [x] Policy-based authorization (`ApiAccess` policy)

### Infrastructure
- [x] `Program.cs` — all services registered and middleware wired
- [x] `EscrowManagerService` — legacy façade (backward compat)

### Testing
- [ ] `HoldFundsHandlerTests` — **STUB** (3 placeholder methods, `Assert.True(true)`)
- [ ] `ReleaseFundsHandlerTests` — **STUB** (3 placeholders)
- [ ] `DisputeFundsHandlerTests` — **STUB** (2 placeholders)
- [ ] `CancelFundsHandlerTests` — **STUB** (4 placeholders)
- [ ] `StripePaymentStrategyTests` — **STUB** (4 placeholders, needs Stripe SDK mocking)

### Webhooks
- [ ] `PaymentIntentEventHandler` — **STUB** (all methods throw NotImplementedException)
- [ ] Stripe webhook signature verification endpoint
- [ ] Webhook event deduplication

## Phase 3: Web MVP Prototype 🔶 PARTIALLY COMPLETE

### Blazor UI
- [x] Blazor Server interactive SSR configured
- [x] Client Dashboard (`/dashboard/client`) — implemented with MediatR
- [x] Consultant Dashboard (`/dashboard/consultant`) — implemented with MediatR
- [x] Transaction Detail (`/transaction/{id}`) — implemented with MediatR
- [x] Login page (`/auth/login`) — **UI ONLY** (no auth logic, disabled buttons)
- [x] Register page (`/auth/register`) — **UI ONLY** (no registration logic)
- [x] Error + NotFound pages (basic)

### Localization
- [x] `IStringLocalizer<SharedResource>` wired in components
- [x] `SharedResource.resx` (en-US) + `SharedResource.es.resx` (es-MX)
- [x] Culture switch endpoint (`/culture/set`)
- [ ] Audit .resx files for completeness (all keys used in components exist?)

### Authentication & Authorization
- [x] API key auth for REST endpoints
- [ ] User authentication (Entra ID / ASP.NET Identity / OIDC) — **NOT IMPLEMENTED**
- [ ] User registration flow — **NOT IMPLEMENTED**
- [ ] Session management for Blazor Server

### Deployment
- [x] Dockerfile at repo root
- [x] docker-compose.yml at repo root
- [ ] Cloud deployment (Azure App Service / Render)
- [ ] Environment variable templates (.env)
- [ ] Production secrets management (Key Vault / user-secrets)
- [ ] CI/CD pipeline (GitHub Actions)

## Backlog (Post-MVP)

- [ ] Web3/Ethereum bridge — wallet signature verification, smart contract integration
- [ ] PayPal payment strategy (`IEscrowPaymentStrategy` implementation)
- [ ] Polly resilience policies on Stripe calls (retry, circuit breaker, bulkhead)
- [ ] Real-time notifications (SignalR or toast system)
- [ ] Admin dashboard for escrow oversight
- [ ] Email notifications on state transitions
- [ ] Audit trail / transaction history log
- [ ] FluentValidation on all MediatR commands
- [ ] Comprehensive test coverage (>90% on payment flows)
- [ ] Performance monitoring (health checks, structured telemetry)
