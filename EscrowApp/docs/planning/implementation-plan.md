# Implementation Plan — NexTruzt.io Escrow Platform

> Last synced with codebase: 2026-04-13

## Revenue Gate

> Every item in this plan was evaluated against the **mvp-gatekeeper** revenue gate:
> _"Does this put money in the bank, or is it engineering vanity?"_
>
> **Day-1 Revenue Model:** Client escrows $5,000 → Stripe holds funds → Work delivered → Funds released
> - Stripe processing: 2.9% + $0.30 = $145.30
> - **NexTruzt.io platform fee: 1.5% = $75.00** ← THIS IS THE PRODUCT
> - Consultant receives: $4,779.70

---

## Decisions Made

| Question | Decision |
|---|---|
| Frontend stack | **Blazor Server** (interactive SSR) — full C# stack |
| Web2 or Web3 | **Web2 first** (Stripe) — Web3 bridge designed but not built |
| Database | **PostgreSQL** (Npgsql) — production-grade from day one |
| Architecture | **Clean Architecture + CQRS/MediatR** — vertical slices |
| CSS framework | **Bootstrap 5** — enterprise LOB UI |
| Auth provider | **ASP.NET Identity** (MVP) — upgrade to Entra ID post-MVP |
| MVP scope method | **mvp-gatekeeper skill** — revenue gate on every feature |

---

## What's Built ✅ (Foundation — Complete)

### Phase 1: Landing Page — ✅ COMPLETE

| Deliverable | Status | Component |
|---|---|---|
| Hero Section | ✅ Done | `Components/Shared/HeroSection.*` |
| How It Works | ✅ Done | `Components/Shared/HowItWorks.*` |
| FAQ Accordion | ✅ Done | `Components/Shared/FaqSection.*` |
| Social Proof | ✅ Done | `Components/Shared/SocialProof.*` |
| NavBar + Footer | ✅ Done | `Components/Shared/NavBar.*`, `Footer.*` |
| Home Orchestrator | ✅ Done | `Components/Pages/Home.*` |

### Phase 2: Escrow Engine — Scaffolding ✅

**Domain Layer:**
- `EscrowTransaction` aggregate (Pending → Held → Released | Disputed)
- `Actor` entity, `IdentityMapping` entity, domain events
- `InMemoryEventBus` (MVP — swappable later)

**Application Layer (MediatR CQRS):**
- `CreateAndHoldFunds`, `HoldFunds`, `ReleaseFunds`, `DisputeFunds` — working handlers
- `GetTransaction` / `ListTransactions` — read queries

**Infrastructure:**
- `StripePaymentStrategy` — full ISP (hold, release, cancel)
- `PaymentStrategyFactory`, `EscrowTransactionRepository`, `EscrowDbContext` + 3 migrations
- `ApiKeyAuthenticationHandler`, `ApiExceptionMiddleware` (RFC 7807)
- `EscrowController` — 6 REST endpoints, Swagger, policy-based auth

### Phase 3: Web UI — Scaffolding ✅

- Client Dashboard (`/dashboard/client`), Consultant Dashboard (`/dashboard/consultant`)
- Transaction Detail (`/transaction/{id}`)
- Login + Register pages (UI shells — no backend)
- Localization (en-US + es-MX), Dockerfile + docker-compose
- CI/CD pipeline (`.github/workflows/ci.yml`) — build + test + coverage

---

## 🚀 MVP Release — Ship-to-Charge

> **7 tasks remaining** (1 of 8 completed) standing between the current codebase and Day-1 revenue.
> Ordered by dependency chain. Complete sequentially unless marked parallel.

### ⚠️ CRITICAL: Platform Fee — Revenue Blocker #1

**Current state:** The 1.5% platform fee is documented in `business-model.md` but **zero fee logic exists in code**. Every transaction today generates $0 revenue for NexTruzt.io.

| # | Task | Revenue Impact | Status |
|---|---|---|---|
| **1** | **Platform fee implementation** | Direct — $0 → $75/tx | ❌ Not started |
| | Add `PlatformFee` + `PlatformFeePercentage` fields to `EscrowTransaction` | | |
| | Fee calculation in `CreateAndHoldFundsHandler` (`amount × 0.015`) | | |
| | EF Core migration for new columns | | |
| | Config: `Platform:FeePercentage` in `appsettings.json` | | |

### MVP Task Queue (Dependency-Ordered)

| # | Task | Depends On | Revenue Gate Justification |
|---|---|---|---|
| **1** | Platform fee (1.5%) | — | No fee = no revenue. #1 blocker. |
| **2** | CancelFunds handler | #1 | Users must cancel escrows — prevents chargebacks |
| **3** | User authentication (ASP.NET Identity) | — | Can't identify users = can't process payments safely |
| **4** | FluentValidation on all commands | #2 | Unvalidated payment amounts = lost money at Stripe |
| **5** | Real unit tests (replace 16 stubs) | #4 | One test per handler proves money flows correctly |
| **6** | Production secrets (env vars) | #5 | Hardcoded mock Stripe key + DB creds = can't deploy | ✅ Done (2026-04-11) |
| **7** | Minimal Stripe webhook | #1 | `payment_intent.succeeded` confirmation (signature verify only) |
| **8** | Cloud deployment | #3, #6 | Can't charge money without a running production server |

### MVP Parallelization

```
Track A (Money Pipe):  #1 Platform Fee → #2 CancelFunds → #4 Validation → #5 Tests → #6 Secrets
Track B (User Access): #3 Auth (parallel with Track A)
Track C (Stripe Sync): #7 Webhook (parallel after #1)
Merge Point:           #8 Cloud Deploy (requires #3 + #6)
```

---

## 📊 Business Strategy & AI Infrastructure — ✅ COMPLETE

### Business Strategy

| Deliverable | Status | Location |
|---|---|---|
| Competitive Analysis | ✅ Done | `docs/business/business-model/business-model.md` (lines ~152-300) |
| Risk Severity Upgrade | ✅ Done | `docs/business/business-model/business-model.md` (lines ~371-385) |
| Strategic Plan | ✅ Done | `docs/business/business-model/strategic-plan.md` |
| Regulatory Compliance Rules | ✅ Done | All 4 instruction files (AGENTS.md, CLAUDE.md, GEMINI.md, copilot-instructions.md) |

**Key Strategic Decisions:**
- 4 GO/NO-GO pre-launch blockers identified (fintech attorney, terminology audit, ToS, Stripe Connect)
- Project repositioned as "escrow-like secure payment holding" to avoid money transmitter licensing
- Revenue projections: Year 1 $81K → Year 2 $360K platform revenue

### AI Infrastructure Export System

| Deliverable | Status | Location |
|---|---|---|
| Export Guide | ✅ Done | `.github/AI-INFRASTRUCTURE-EXPORT-GUIDE.md` (778 lines) |
| Export Script | ✅ Done | `.github/scripts/export-ai-infrastructure.ps1` |
| Tailoring Wizard | ✅ Done | `.github/scripts/tailor-ai-infrastructure.ps1` |

**Portability:** 43 skills, 7 extensions, 10 rules, 10 hooks inventoried. ~88% domain-agnostic and portable.

### AI Compatibility Audit (4-Tool)

| Deliverable | Status | Location |
|---|---|---|
| Copilot CLI Audit | ✅ 100% | `.github/AI-COMPATIBILITY-AUDIT.md` |
| Claude Code Audit | ✅ 100% | `.github/AI-COMPATIBILITY-AUDIT.md` |
| Gemini/Antigravity Audit | ✅ 100% | `.agent/rules/` (11), `.agent/workflows/` (4), `.gemini/settings.json` |
| Codex CLI Audit | ✅ 100% (was 65%) | Subdirectory `AGENTS.md` (5), `config.toml` context section |
| CODEX.md | ✅ Created | `CODEX.md` (200 lines) |
| .codex/ config | ✅ Created | `.codex/config.toml`, `.codex/README.md` |
| .claudeignore | ✅ Created | `.claudeignore` |
| Doc drift fixes | ✅ Fixed | 5 files updated (skill counts 36/41 → 43) |
| Audit report | ✅ Created | `.github/AI-COMPATIBILITY-AUDIT.md` (260 lines) |

**Result:** All 4 major agentic AI tools fully supported. Universal assets (AGENTS.md + 43 skills) work across all tools.

---

## 📋 Post-MVP Backlog — Explicitly Deferred

> These items **failed the revenue gate**. Each has an explicit **upgrade trigger** — do not build until the trigger condition is met.

| Item | Upgrade Trigger | Why Deferred |
|---|---|---|
| Email notifications | User retention drops below 60% | Users see status in dashboard; email infra is a rabbit hole |
| Polly resilience (retry/circuit breaker) | Transaction volume > 100/day | <10 users = manual retry if Stripe fails |
| Comprehensive test coverage (>90%) | Next feature sprint begins | One test per handler is MVP; full coverage is ongoing |
| Web3/Ethereum bridge | 3+ users request crypto escrow | Zero proven demand; Stripe handles all payments today |
| PayPal payment strategy | 5+ users request PayPal | Second provider doubles integration complexity |
| Admin dashboard | Admin operations > 10/week | Use PostgreSQL queries directly until then |
| .resx localization audit | User reports missing translation | Missing keys show key name — ugly but functional |
| Real-time notifications (SignalR) | 100+ concurrent users | Page refresh works at <100 users |
| Formal audit trail | Regulatory compliance required | DB records provide basic audit; formal log is v2 |
| Performance monitoring | Production deployment stable | Premature optimization without production traffic data |
| UI polish (toasts, spinners) | User feedback requests it | Bootstrap defaults ship; custom polish doesn't |

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

**Tech Stack:**
- .NET 10, Blazor Server (interactive SSR)
- PostgreSQL + EF Core (Npgsql)
- Stripe SDK (PaymentIntents, manual capture)
- MediatR (CQRS vertical slices)
- Bootstrap 5 (enterprise UI)
- xUnit + FluentAssertions + Moq
- Docker (Dockerfile + docker-compose)
- GitHub Actions CI/CD

---

## MVP Ship Checklist

> All 10 gates must be green before declaring MVP complete. From `mvp-gatekeeper` skill.

- [ ] **Money flows** — Stripe PaymentIntent hold → capture works with test card `4242...`
- [ ] **Platform fee collected** — 1.5% fee calculated, stored, and visible in transaction
- [ ] **Full lifecycle** — Create → Hold → Release AND Create → Hold → Cancel both work
- [ ] **Auth blocks strangers** — `[Authorize]` on every page/endpoint, login/register functional
- [ ] **Bad input rejected** — FluentValidation on all 5 payment commands
- [ ] **Idempotency keys present** — every payment mutation is retry-safe
- [ ] **No secrets in code** — zero hardcoded keys/creds in source
- [ ] **HTTPS enforced** — HSTS + redirect in production config
- [ ] **Errors don't crash** — friendly error message, not stack trace
- [ ] **Tests pass** — `dotnet test` green with real assertions
