# Implementation Plan — NexTruzt.io Escrow Platform

> Last synced with codebase: 2026-04-16 03:00
> 
> **Current Status:** Track A (#1-2) ✅ 100% complete. Track B (#3) ✅ **COMPLETE — 14 of 14 slices done (100%)**. Track C (#7/#8) ready to start.
> All documentation (#3, #7, #8) completed. Continuing with incremental-implementation skill (TDD + vertical slices).

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

### ⚠️ CRITICAL: Platform Fee — ✅ DONE (2026-04-14)

**Previous state:** The 1.5% platform fee was documented in `business-model.md` but had zero fee logic in code. Every transaction generated $0 revenue for NexTruzt.io.

**Completed:**
- `EscrowTransaction` — `PlatformFee` + `PlatformFeePercentage` fields added
- `Infrastructure/Options/PlatformOptions.cs` — typed Options record, bound from `Platform` config section
- `appsettings.json` + `appsettings.Production.json` — `Platform` section with 1.5% default
- `Program.cs` — `services.Configure<PlatformOptions>` registered
- `CreateAndHoldFundsHandler` — fee = `max(amount × 0.015, $0.50)`; Stripe charged `escrowAmount + platformFee`
- `PaymentReceivedEvent` — extended with `PlatformFee` + `PlatformFeePercentage` for audit trail
- `EscrowTransactionResponse` — `PlatformFee`, `PlatformFeePercentage`, `TotalCharged` surfaced to callers
- EF Core migration `AddPlatformFeeToEscrowTransaction` — created and ready to apply

> ⚠️ **Compliance-sensitive** — requires legal review before production deployment.

### MVP Task Queue (Dependency-Ordered)

| # | Task | Depends On | Revenue Gate Justification |
|---|---|---|---|
| **1** | ~~Platform fee (1.5%)~~ | — | ✅ Done 2026-04-14 |
| **2** | ~~CancelFunds handler~~ | #1 | ✅ Done 2026-04-14 |
| **3** | User authentication (ASP.NET Identity) | — | Can't identify users = can't process payments safely |
| **4** | ~~FluentValidation on all commands~~ | #2 | ✅ Done 2026-04-16 — Unvalidated payment amounts = lost money at Stripe |
| **5** | Real unit tests (replace 16 stubs) | #4 | One test per handler proves money flows correctly | ✅ Done (2026-04-16) |
| **6** | Production secrets (env vars) | #5 | Hardcoded mock Stripe key + DB creds = can't deploy | ✅ Done (2026-04-16) |
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
- [x] **Platform fee collected** — 1.5% fee calculated, stored, and visible in transaction ✅ 2026-04-14
- [ ] **Full lifecycle** — Create → Hold → Release AND Create → Hold → Cancel both work
  - [x] Cancel code implemented and event published ✅ 2026-04-14
  - [x] Release code fixed (status string bug) ✅ 2026-04-14
- [ ] **Auth blocks strangers** — `[Authorize]` on every page/endpoint, login/register functional
- [ ] **Bad input rejected** — FluentValidation on all 5 payment commands
- [ ] **Idempotency keys present** — every payment mutation is retry-safe
- [ ] **No secrets in code** — zero hardcoded keys/creds in source
- [ ] **HTTPS enforced** — HSTS + redirect in production config
- [ ] **Errors don't crash** — friendly error message, not stack trace
- [ ] **Tests pass** — `dotnet test` green with real assertions

---

## 🚀 Next Steps — Ready to Implement

### Track A (Money Pipe) — ✅ 100% COMPLETE

| # | Task | Complexity | Dependencies | Status |
|---|------|-----------|--------------|--------|
| **#1** | ~~Platform Fee (1.5%)~~ | L | — | ✅ Done 2026-04-14 |
| **#2** | ~~CancelFunds handler~~ | M | #1 | ✅ Done 2026-04-14 |
| **#4** | ~~FluentValidation on all commands~~ | M | #2 | ✅ Done 2026-04-16 |
| **#5** | ~~Real unit tests (17 stubs)~~ | L | #4 | ✅ Done 2026-04-16 |
| **#6** | ~~Production secrets (env vars)~~ | S | #5 | ✅ Done 2026-04-16 |

**Unblocks:** Track B (#3), Track C (#7, #8)

### Track B (User Access) — 📋 READY

| # | Task | Complexity | Dependencies | Status | Documentation |
|---|------|-----------|--------------|--------|-----------------|
| **#3** | ASP.NET Identity (email/password auth) | L | None (parallel) | 📋 READY | [`aspnet-identity-mvp.md`](cross-cutting/authentication/aspnet-identity-mvp.md) |

**Scope:** User registration + login, Blazor Server auth integration, `ApplicationUser` + IdentityDbContext, password hashing, session management

**Subtasks:** 14 checkbox items in task-checklist.md (NuGet packages, migration, Program.cs config, Login/Register pages, tests)

**Unblocks:** #8 Cloud deployment (merge point)

### Track C (Stripe Sync) — 📋 READY

| # | Task | Complexity | Dependencies | Status | Documentation |
|---|------|-----------|--------------|--------|-----------------|
| **#7** | Minimal Stripe webhook (`payment_intent.succeeded`) | M | #1 (parallel after) | 📋 READY | [`minimal-webhook-handler-mvp.md`](../../architecture/stripe-webhooks/minimal-webhook-handler-mvp.md) |

**Scope:** Webhook endpoint (POST /api/webhooks/stripe), signature verification, event handler for `payment_intent.succeeded` only (other events deferred)

**Subtasks:** 10 checkbox items in task-checklist.md (endpoint, verifier, handler, tests, local Stripe CLI testing)

**Unblocks:** Real-time payment confirmation logging

### Merge Point (Production Ready) — 📋 READY

| # | Task | Complexity | Dependencies | Status | Documentation |
|---|------|-----------|--------------|--------|-----------------|
| **#8** | Cloud deployment (Azure Container Apps) | L | #3 + #6 + #7 (opt) | 📋 READY | [`cloud-deployment-steps-mvp.md`](operations/deployment/cloud-deployment-steps-mvp.md) |

**Scope:** Azure resource setup (ACR, PostgreSQL, Key Vault, Container Apps), deployment, health checks, monitoring, rollback procedure

**Subtasks:** 15 checkbox items in task-checklist.md (resource creation, Docker push, migrations, webhook config, smoke tests, Application Insights)

**Pre-Launch Blockers:** 
- 🔴 Fintech attorney review (12 weeks)
- 🔴 Money transmitter license assessment
- 🔴 Terms of Service approval
- 🔴 "Escrow" terminology audit

## Parallelization Strategy

```
Start: 2026-04-16 (after Track A completion)

Track B (#3):    [=================== 2–3 weeks ===================]
                 Register → Login → Blazor auth integration → 14 tests
                 
Track C (#7):    [============ 1–2 weeks after start =============]
                 Webhook endpoint → Signature verify → Event handler → Tests
                 
        ↓ (when both #3 + #6 done)
        
Merge (#8):      [==================== 1–2 weeks ====================]
                 Azure setup → Deploy → Health checks → Smoke tests → Production
                 
Post-MVP Legal:  [======== 8–12 weeks (start NOW, parallel) ========]
                 Attorney review → License assessment → ToS approval → Terminology audit
```

## Documentation Ready

All three next steps have comprehensive implementation guides:

| Document | Lines | Coverage |
|-----------|-------|----------|
| **#3 — ASP.NET Identity** | 320 | Database schema, DI registration, Blazor config, Login/Register patterns, security guardrails, testing strategies, post-MVP enhancements |
| **#7 — Stripe Webhook** | 420 | Architecture (endpoint + verifier + handler), MVP scope (only `payment_intent.succeeded`), code examples, local testing with Stripe CLI, post-MVP events |
| **#8 — Cloud Deployment** | 370 | Azure step-by-step (ACR, PostgreSQL, Key Vault, Container Apps), alternative platforms (AWS/GCP), post-launch verification, monitoring, cost estimation, rollback procedure |

## Definition of Done

When moving to the next phase, update:
1. `task-checklist.md` — Mark all subtasks `[x]` and add completion date
2. `implementation-plan.md` — Update this section with ✅ status
3. Review for compliance:
   - All tests passing (`dotnet test`)
   - All docs updated (`docs-status` tool shows no stale docs)
   - No hardcoded secrets
   - Code-behind pattern on all Blazor components
   - FluentValidation on all commands
   - MediatR handlers + events properly designed
