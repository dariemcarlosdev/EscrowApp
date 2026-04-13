# 00 — Architecture Overview

> **NexTruzt.io** — A fintech escrow platform for independent consultants and clients
> to securely hold and release payments.

## Status: Implemented (MVP)

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Presentation Layer                          │
│  Blazor Server (.razor + .razor.cs + .razor.css)                │
│  IStringLocalizer<T> · Bootstrap 5 · Scoped CSS                 │
│  Pages: Home, NavBar, HeroSection, HowItWorks, SocialProof,    │
│         FaqSection, Footer, ClientDashboard, ConsultantDashboard│
│         TransactionDetail, Login, Register                      │
└────────────────────────────┬────────────────────────────────────┘
                             │  MediatR.Send(Command)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
│  MediatR Handlers (CQRS — Vertical Slice Architecture)          │
│  Features/Escrow/HoldFunds/  ReleaseFunds/  DisputeFunds/       │
│  CancelFunds/  Webhooks/  CreateAndHoldFunds/                   │
│  Commands · Handlers · Result DTOs                              │
│  IEscrowManagerService (legacy facade)                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
┌──────────────────┐ ┌─────────────┐ ┌─────────────────┐
│  Infrastructure  │ │  Strategies │ │   Event Bus     │
│  EscrowDbContext  │ │  IFundHold  │ │  IEventBus      │
│  PostgreSQL       │ │  IFundRel   │ │  InMemoryEvent  │
│  EF Core          │ │  IFundCanc  │ │  Bus (MVP)      │
│  Repository Pat.  │ │  Stripe…    │ │                 │
└──────────────────┘ └─────────────┘ └─────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                              │
│  Models: EscrowTransaction, Actor, IdentityMapping              │
│  Events: DomainEvent, PaymentReceivedEvent, DisputeRaisedEvent  │
│  No framework dependencies — pure C# POCOs                      │
└─────────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

| Layer            | Responsibility                                          | Key Namespaces                           |
| ---------------- | ------------------------------------------------------- | ---------------------------------------- |
| **Presentation** | Blazor Server UI, localization, component composition   | `Components.Pages`, `Components.Layout`  |
| **Application**  | MediatR commands/handlers, orchestration, result DTOs   | `Features.Escrow.*`                      |
| **Infrastructure** | EF Core, PostgreSQL, Stripe SDK, repository impl.     | `Data`, `Data.Repositories`              |
| **Domain**       | Entity models, domain events, strategy interfaces       | `Models`, `Events`, `Services.Strategies`|

## Dependency Flow

```
Presentation ──► Application ──► Domain
                      │
                      ▼
               Infrastructure
```

> Inner layers never reference outer layers.
> `Infrastructure` implements interfaces defined in the Domain/Application layers.

## Design Patterns

| Pattern                | Where Used                                     | Purpose                                  |
| ---------------------- | ---------------------------------------------- | ---------------------------------------- |
| **MediatR (CQRS)**    | `Features/Escrow/*/`                           | Decouple UI from business logic          |
| **Strategy**           | `Services/Strategies/`                         | Multi-provider payment support (OCP)     |
| **Factory**            | `PaymentStrategyFactory`                       | Resolve strategy at runtime by provider  |
| **Repository**         | `IEscrowTransactionRepository`                 | Abstract data access behind interface    |
| **Event Bus**          | `IEventBus` / `InMemoryEventBus`               | Decouple side effects from core flow     |
| **ISP (Segregation)**  | `IFundHoldable`, `IFundReleasable`, `IFundCancellable` | Capabilities vary per provider   |
| **Code-Behind**        | `.razor` + `.razor.cs` + `.razor.css`          | Separate markup, logic, and styles       |

## Technology Stack

| Technology       | Version / Notes                                |
| ---------------- | ---------------------------------------------- |
| .NET             | 10 (Preview)                                   |
| Blazor Server    | Interactive server-side rendering              |
| PostgreSQL       | Primary data store via EF Core                 |
| Stripe SDK       | PaymentIntent API with manual capture          |
| MediatR          | CQRS command/query dispatching                 |
| Bootstrap 5      | UI framework with custom scoped CSS            |
| IStringLocalizer | .resx-based localization (en, es)              |

## Key Source Files

| File                          | Purpose                              |
| ----------------------------- | ------------------------------------ |
| `Program.cs`                  | DI registration, middleware pipeline |
| `Data/EscrowDbContext.cs`     | EF Core context, unique constraints  |
| `Models/EscrowTransaction.cs` | Core escrow entity                   |
| `Services/Strategies/`        | Payment strategy interfaces + impls  |
| `Features/Escrow/`            | MediatR vertical slices              |
| `Events/`                     | Domain events + event bus            |
| `Resources/`                  | Localization .resx files             |

## Infrastructure — Webhooks

```
Infrastructure/Webhooks/Stripe/
├── StripeWebhookEndpoint.cs     ← Minimal API endpoint (transport)
└── StripeSignatureVerifier.cs   ← HMAC signature verification

Features/Escrow/Webhooks/
└── PaymentIntentEventHandler.cs ← Business logic (state transitions)
```

Webhooks are split: transport/verification in Infrastructure, business logic in Application layer.

## Deployment

| Artifact | Purpose |
|----------|---------|
| `Dockerfile` | Multi-stage build (SDK → runtime, non-root) |
| `docker-compose.yml` | Local dev: app + PostgreSQL |
| `.github/workflows/ci.yml` | CI: build, test, coverage |

## Testing

Test project at `EscrowApp.Tests/` — xUnit + FluentAssertions + Moq.
All test files currently contain **skeleton placeholders** — real test implementations are pending (MVP Task #5).
See [Testing Strategy](cross-cutting/testing/testing-strategy.md) for full strategy.

## Security Hardening

The following security measures were added in the 2026-04-11 audit:

| Measure | Implementation |
|---------|---------------|
| **Security headers** | Custom middleware: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies: none`, Content-Security-Policy |
| **BREACH protection** | Response compression disabled for HTTPS requests |
| **Timing-safe auth** | `ApiKeyAuthenticationHandler` uses `CryptographicOperations.FixedTimeEquals()` |
| **Cookie security** | Culture cookies: `Secure = true`, `HttpOnly = true`, `SameSite = Lax` |
| **Secrets removed** | `appsettings.json` and `appsettings.Development.json` cleared of hardcoded secrets |
| **[Authorize] enforced** | Re-enabled on all dashboard pages (Client, Consultant, TransactionDetail) |

## MediatR Pipeline Behaviors

| Behavior | Purpose |
|----------|---------|
| `LoggingBehavior<,>` | Structured logging for all MediatR requests |
| `PerformanceBehavior<,>` | Alerts on slow-running handlers |

## Configuration

- **Database**: `ConnectionStrings:DefaultConnection` — must be set via user-secrets or environment variable
- **Stripe**: `Stripe:SecretKey` — must be set via user-secrets or environment variable
- **Stripe Return URL**: `Stripe:PaymentReturnUrl` — configurable 3D Secure redirect URL
- **Stripe Webhooks**: `Stripe:WebhookSecret` — HMAC verification key
- **API Keys**: `ApiKeys:{clientId}:Key` — via options pattern (`ApiKeySettings` / `ApiKeyConfig`)
- **Localization**: Supported cultures `en-US`, `es-MX` — cookie-based switching via `/culture/set`

## Documentation Index

| Category | Doc | Topic |
|----------|-----|-------|
| Architecture | `architecture/overview` | Architecture Overview (this file) |
| Architecture | `architecture/payment-strategies` | Strategy Pattern + ISP interfaces |
| Architecture | `architecture/event-bus` | Domain events + IEventBus |
| Architecture | `architecture/api-integration` | REST API + Swagger |
| Architecture | `architecture/stripe-webhooks` | Stripe webhook handling |
| Features | `features/hold-funds` | Hold Funds (Stripe manual capture) |
| Features | `features/release-funds` | Release Funds (capture) |
| Features | `features/dispute-funds` | Dispute Funds (cancel + refund) |
| Features | `features/cancel-funds` | Cancel Funds (void hold) |
| Features | `features/landing-page` | Landing page components |
| Features | `features/client-dashboard` | Client dashboard |
| Features | `features/consultant-dashboard` | Consultant dashboard |
| Features | `features/transaction-detail` | Transaction detail view |
| Cross-cutting | `cross-cutting/hybrid-identity` | Actor model + identity mapping |
| Cross-cutting | `cross-cutting/localization` | i18n/l10n setup |
| Cross-cutting | `cross-cutting/testing` | Test strategy |
| Audits | `audits/security-audit` | OWASP audit findings |
| Audits | `audits/compliance-audit` | Compliance audit log |
| Operations | `operations/deployment` | Deployment strategy |
| Business | `business/business-model` | Revenue model + pricing |
| Planning | `planning/implementation-plan` | Implementation plan |
| Planning | `planning/task-checklist` | Execution checklist |
