# Implementation Plan — NexTruzt.io Escrow Platform

> Last synced with codebase: 2026-04-09

## Decisions Made

These questions from the original plan have been answered by implementation:

| Question | Decision |
|---|---|
| Frontend stack | **Blazor Server** (interactive SSR) — full C# stack |
| Web2 or Web3 | **Web2 first** (Stripe) — Web3 bridge designed but not built |
| Database | **PostgreSQL** (Npgsql) — production-grade from day one |
| Architecture | **Clean Architecture + CQRS/MediatR** — vertical slices |
| CSS framework | **Bootstrap 5** — enterprise LOB UI |

---

## Phase 1: Landing Page — ✅ COMPLETE

All landing page components built with Blazor Server, code-behind pattern, scoped CSS, and i18n.

| Deliverable | Status | Component |
|---|---|---|
| Hero Section | ✅ Done | `Components/Shared/HeroSection.*` |
| How It Works | ✅ Done | `Components/Shared/HowItWorks.*` |
| FAQ Accordion | ✅ Done | `Components/Shared/FaqSection.*` |
| Social Proof | ✅ Done | `Components/Shared/SocialProof.*` |
| NavBar + Footer | ✅ Done | `Components/Shared/NavBar.*`, `Footer.*` |
| Home Orchestrator | ✅ Done | `Components/Pages/Home.*` |
| Cloud Deployment | ❌ Pending | Dockerfile exists, no cloud hosting yet |

---

## Phase 2: Escrow Engine — 🔶 ~85% COMPLETE

### What's Built ✅

**Domain Layer:**
- `EscrowTransaction` aggregate with status state machine (Pending → Held → Released \| Disputed)
- `Actor` entity (Web2/Web3 hybrid identity)
- `IdentityMapping` entity (multi-provider: Email, Google, MetaMask, WalletConnect)
- Domain events: `PaymentReceivedEvent`, `DisputeRaisedEvent`
- `InMemoryEventBus` (MVP — swappable for MassTransit later)

**Application Layer (MediatR CQRS):**
- `CreateAndHoldFunds` — create + hold atomically
- `HoldFunds` — hold on existing transaction
- `ReleaseFunds` — capture held PaymentIntent
- `DisputeFunds` — cancel hold + dispute flag
- `GetTransaction` / `ListTransactions` — read queries

**Infrastructure:**
- `StripePaymentStrategy` — full ISP implementation (hold, release, cancel)
- `PaymentStrategyFactory` — OCP runtime resolution
- `EscrowTransactionRepository` — EF Core with pagination + filters
- `EscrowDbContext` + 3 migrations (PostgreSQL)
- `ApiKeyAuthenticationHandler` — X-Api-Key header validation
- `ApiExceptionMiddleware` — RFC 7807 error responses

**API:**
- `EscrowController` — 6 REST endpoints (`/api/escrow/*`)
- Swagger/OpenAPI with API key security scheme
- Policy-based authorization (`ApiAccess`)

### What's Missing ❌

| Item | Priority | Effort | Notes |
|---|---|---|---|
| `CancelFunds` handler | High | ~1 hour | Stub exists; mirror DisputeFunds logic |
| Webhook handlers | High | ~3 hours | `PaymentIntentEventHandler` is stub; needs Stripe signature verification |
| Unit tests | High | ~4 hours | 16 test methods exist as stubs (`Assert.True(true)`); need real Arrange-Act-Assert |
| FluentValidation | Medium | ~2 hours | No validators on commands yet |

---

## Phase 3: Web MVP — 🔶 ~60% COMPLETE

### What's Built ✅

| Deliverable | Status | Route |
|---|---|---|
| Client Dashboard | ✅ Done | `/dashboard/client` |
| Consultant Dashboard | ✅ Done | `/dashboard/consultant` |
| Transaction Detail | ✅ Done | `/transaction/{id}` |
| Login Page | 🔶 UI Only | `/auth/login` (no auth backend) |
| Register Page | 🔶 UI Only | `/auth/register` (no auth backend) |
| Localization (en/es) | ✅ Wired | `IStringLocalizer` + .resx files + culture switch |
| Dockerfile | ✅ Done | Multi-stage build at repo root |
| docker-compose.yml | ✅ Done | App + PostgreSQL |

### What's Missing ❌

| Item | Priority | Effort | Notes |
|---|---|---|---|
| User authentication | **Critical** | ~6 hours | Entra ID or ASP.NET Identity; Login/Register wired but non-functional |
| Cloud deployment | High | ~2 hours | Dockerfile exists; needs Azure App Service or Render config |
| .resx audit | Medium | ~1 hour | Verify all component keys exist in both en/es files |
| CI/CD pipeline | Medium | ~2 hours | GitHub Actions for build + test + deploy |
| Production secrets | Medium | ~1 hour | Move API keys from appsettings to Key Vault / env vars |
| Polish (toasts, spinners) | Low | ~2 hours | Some loading states exist; needs consistent UX |

---

## Architecture Summary

```
Components/     Blazor pages + shared components (code-behind pattern)
    │
    ▼  IMediator.Send()
Features/       MediatR vertical slices (command/query handlers)
    │
    ▼  IEscrowTransactionRepository, IFundHoldable, IEventBus
Models/Events/  Domain entities + events (zero framework dependencies)
    ▲
    │  implements interfaces
Data/Services/  EF Core repository, Stripe strategy, InMemoryEventBus
Infrastructure/ ApiKey auth, exception middleware
```

**Tech Stack Finalized:**
- .NET 10, Blazor Server (interactive SSR)
- PostgreSQL + EF Core (Npgsql)
- Stripe SDK (PaymentIntents, manual capture)
- MediatR (CQRS vertical slices)
- Bootstrap 5 (enterprise UI)
- xUnit + FluentAssertions (test stubs in place)
- Docker (Dockerfile + docker-compose)

---

## MVP Completion Priorities

> Ordered by impact and dependency. Complete these to reach a shippable MVP.

1. **Implement CancelFunds handler** — unblock full escrow lifecycle
2. **Write unit tests** — 16 stubs ready; need real assertions with Moq
3. **Implement user authentication** — Login/Register pages are UI-only
4. **Implement Stripe webhooks** — async payment state sync
5. **Deploy to cloud** — Docker ready; needs hosting config
6. **Add FluentValidation** — input validation on all commands
7. **CI/CD pipeline** — automate build → test → deploy

---

## Verification Checklist

- [ ] All 6 API endpoints return correct responses via Swagger
- [ ] Stripe test card (`4242...`) completes hold → release flow
- [ ] Dispute flow cancels PaymentIntent and updates status
- [ ] Cancel flow voids held funds (after handler implementation)
- [ ] Unit tests pass with `dotnet test` (after test implementation)
- [ ] Docker Compose starts app + PostgreSQL successfully
- [ ] Localization switches between en-US and es-MX correctly
- [ ] All Blazor pages render without errors
