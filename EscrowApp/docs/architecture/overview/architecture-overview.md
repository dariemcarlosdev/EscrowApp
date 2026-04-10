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
See `docs/16-Testing/` for full strategy.

## Configuration

- **Database**: `ConnectionStrings:DefaultConnection` in `appsettings.json`
- **Stripe**: `Stripe:SecretKey` — must be set via user-secrets or environment variable in production
- **Stripe Webhooks**: `Stripe:WebhookSecret` — HMAC verification key
- **Localization**: Supported cultures `en-US`, `es-MX` — cookie-based switching via `/culture/set`

## Documentation Index

| Doc | Topic |
|-----|-------|
| 00 | Architecture Overview (this file) |
| 01-09 | Core features (Hold, Release, Dispute, Strategies, Identity, Events, i18n, UI, API) |
| 10 | Security Audit |
| 11 | Cancel Funds |
| 12 | Stripe Webhooks |
| 13-15 | Dashboard pages (Client, Consultant, Transaction Detail) |
| 16 | Testing Strategy |
| 17 | Deployment |
| 18 | Business Model & Revenue |
