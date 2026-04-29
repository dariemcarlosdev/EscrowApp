# Copilot Instructions — NexTruzt.io EscrowApp

> Master project-level instructions for GitHub Copilot and all AI coding assistants.

## Project

**NexTruzt.io EscrowApp** — A fintech platform providing **escrow-like secure payment holding** for independent consultants and clients via Stripe Connect delayed payouts, with a planned Web3/Ethereum bridge.

> ⚠️ **Regulatory notice:** NexTruzt.io is **not a licensed escrow agent**. The term "escrow" is used internally for engineering context only — it **must not** appear in user-facing UI, marketing, or legal copy without explicit fintech attorney approval. See **Regulatory Compliance — CRITICAL** section below.

**Tech Stack:**
- .NET 10, Blazor Server (interactive SSR)
- PostgreSQL with EF Core (Npgsql)
- Stripe SDK (PaymentIntents, manual capture)
- MediatR (CQRS vertical slices)
- Bootstrap 5 (enterprise LOB UI)
- IStringLocalizer with .resx files (en-US, es-MX)

---

## Architecture

**Clean Architecture** with **CQRS** organized as vertical slices.

### Layer Map

```
Presentation     Components/              Blazor pages, layouts, scoped CSS
Application      Features/Escrow/         MediatR command/query handlers
Domain           Models/                  EscrowTransaction, Actor, IdentityMapping
                 Events/                  DomainEvent, IEventBus, domain event classes
                 Services/Strategies/     IFundHoldable, IFundReleasable, IFundCancellable
Infrastructure   Data/                    EscrowDbContext, IEscrowTransactionRepository impl
                 Services/               StripeEscrowService, EscrowManagerService
                 Infrastructure/Auth/     ApiKeyAuthenticationHandler
                 Infrastructure/Middleware/ ApiExceptionMiddleware
```

### Dependency Direction — MANDATORY

```
Components/ ──→ Features/ ──→ Models/    ←── Data/
                              Events/    ←── Services/
                              Strategies/ ←── Infrastructure/
```

Inner layers (Models, Events, Strategies) **never** reference outer layers. Infrastructure implements domain interfaces.

---

## Design Patterns

| Pattern | Where | Purpose |
|---|---|---|
| **Strategy** | `Services/Strategies/` | Payment provider abstraction (Stripe, future PayPal/ETH) |
| **Repository** | `Data/Repositories/` | Data access abstraction — EF Core hidden from business logic |
| **Factory** | `IPaymentStrategyFactory` | Runtime resolution of payment strategy by provider name |
| **Event Bus** | `Events/IEventBus` | Decouple side effects from business operations |
| **MediatR/CQRS** | `Features/Escrow/` | Separate command (write) and query (read) paths |
| **Vertical Slice** | `Features/Escrow/*/` | Each feature is a self-contained slice: command + handler + (optional validator) |

### Strategy Interfaces (ISP-Compliant)

```csharp
IEscrowPaymentStrategy          // Marker — every provider implements this
├── IFundHoldable               // HoldFundsAsync(amount, paymentMethodId, idempotencyKey)
├── IFundReleasable             // ReleaseFundsAsync(externalReference, idempotencyKey)
└── IFundCancellable            // CancelHoldAsync(externalReference, idempotencyKey)
```

Providers implement only the capabilities they support. Stripe implements all three. A future crypto provider might only implement `IFundHoldable`.

---

## Blazor Rules — MANDATORY

### Code-Behind Pattern (Always)

Every component produces **three files**:

```
ComponentName.razor       ← Markup only. No @code {} blocks. Ever.
ComponentName.razor.cs    ← sealed partial class. All logic here.
ComponentName.razor.css   ← Scoped CSS. Bootstrap 5 + custom overrides.
```

### Component Conventions

- Inject services via `[Inject]` in code-behind — not `@inject` in markup (markup `@inject` is acceptable for `IStringLocalizer` only).
- Use `IStringLocalizer<SharedResource>` for all user-facing text.
- Use `IMediator` for all data operations — never call repositories or services directly from components.
- Use `[CascadingParameter] Task<AuthenticationState>` for auth state.
- Implement `IDisposable` / `IAsyncDisposable` when using event handlers or JS interop.
- Override `OnInitializedAsync` for data loading — not the constructor.

---

## Security — OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control** | `[Authorize]` on every endpoint. Policy-based auth (`"ApiAccess"`). Default deny. |
| **Cryptographic Failures** | Secrets via env vars or Key Vault. Never in source or `appsettings.json`. |
| **Injection** | Parameterized queries only (EF Core). No raw SQL string concatenation. |
| **Insecure Design** | Strategy Pattern enforces payment provider boundaries. |
| **Security Misconfiguration** | HTTPS + HSTS enforced. Antiforgery tokens. Swagger only in Development. |
| **Vulnerable Components** | Keep NuGet packages updated. Monitor for CVEs. |
| **Auth Failures** | API key via `X-Api-Key` header. Validate every request. |
| **Logging Failures** | Structured logging. Correlation IDs. **Never log PII, tokens, or secrets.** |

---

## Payment Rules — MANDATORY

1. **Idempotency keys** on every payment operation. All strategy methods require an `idempotencyKey` parameter.
2. **Manual capture** via Stripe PaymentIntents — authorize, hold, then explicitly capture on release.
3. **Never modify amounts** between authorization and capture. Amounts come from the domain model.
4. **Dispute blocks release** — a disputed transaction cannot transition to Released.
5. **Domain events after persistence** — publish `PaymentReceivedEvent` or `DisputeRaisedEvent` only after `SaveChangesAsync`.
6. **ExternalReference** stores the Stripe PaymentIntent ID (or future ETH tx hash) for reconciliation.

---

## Regulatory Compliance — CRITICAL

> 🔴 **Applies to every feature implementation. Non-negotiable.**

NexTruzt.io is **NOT** a licensed escrow agent or money transmitter. It provides escrow-_like_ payment holding via Stripe Connect. This affects every payment feature, UI string, and user-facing copy.

### Mandatory Checks on Compliance-Sensitive Changes

Changes touching **payments, payouts, fees, disputes, onboarding, KYC, or user-facing payment copy** require:

1. **Flag regulatory impact** — State which compliance area is affected before implementing.
2. **Never introduce "escrow" in user-facing copy** — Use: "secure payment holding", "payment protection", "held funds", "platform fee". The word "escrow" in `.resx`, `.razor`, error messages, or any user-visible string requires legal review.
3. **Note compliance checkpoint** — After completion: _"⚠️ Compliance-sensitive — requires legal review before production."_
4. **Never claim regulatory status** — No code, UI, or docs may imply NexTruzt.io is a licensed escrow agent, money transmitter, or bank.
5. **Preserve audit trails** — Every payment state transition must emit domain events for regulatory traceability.

### User-Facing Terminology

| ❌ Do NOT use | ✅ Use instead |
|---|---|
| "Escrow account/service/agent" | "Secure payment holding" / "Payment protection" |
| "Escrowed funds" | "Held funds" / "Secured funds" |
| "Escrow fee" | "Platform fee" / "Service fee" |

> **Internal code:** "Escrow" is acceptable in code identifiers, namespaces, and internal docs — it describes the pattern. It must not leak into user-visible surfaces.

See `docs/business/business-model/strategic-plan.md` for the full pre-launch compliance action register.

---

## CQRS Flow

All business operations go through MediatR:

```
UI/API  ──→  IMediator.Send(Command/Query)
               │
               ▼
         Handler (Features/Escrow/*/Handler.cs)
               │
               ├──→ Validate input
               ├──→ Resolve strategy (IPaymentStrategyFactory)
               ├──→ Execute payment op (IFundHoldable, etc.)
               ├──→ Persist via IEscrowTransactionRepository
               ├──→ Publish domain event (IEventBus)
               └──→ Return result
```

### Existing Slices

| Slice | Type | Purpose |
|---|---|---|
| `CreateAndHoldFunds/` | Command | Create transaction + hold funds atomically |
| `HoldFunds/` | Command | Hold funds on existing transaction |
| `ReleaseFunds/` | Command | Capture held funds |
| `DisputeFunds/` | Command | Flag transaction as disputed |
| `GetTransaction/` | Query | Read single transaction by ID |
| `ListTransactions/` | Query | List transactions with filtering |

---

## Code Conventions

| Convention | Rule |
|---|---|
| Namespaces | File-scoped (`namespace EscrowApp.X;`) |
| Nullability | Enabled — use `string?` for nullable |
| Inheritance | `sealed` by default on concrete classes |
| DTOs | `record` types with `init` properties |
| Async | `async Task` / `async Task<T>` with `CancellationToken` |
| Naming | Intention-revealing. No abbreviations except DTO, ID, HTTP. |
| Guard clauses | Fail fast at method entry — no deep nesting |
| Constants | No magic strings or numbers — use `const` or `enum` |

---

## Localization

- **Resource files:** `Resources/SharedResource.resx` (en-US default), `SharedResource.es.resx` (es-MX)
- **Component resources:** `Resources/Components/` for component-specific strings
- **Injection:** `IStringLocalizer<SharedResource>` in code-behind files
- **Markup:** `@L["KeyName"]` for localized strings
- **Culture switch:** `GET /culture/set?culture={code}&redirectUri={path}` — cookie-based
- **All user-facing strings must be localized** — no hardcoded text in `.razor` or `.razor.cs` files

---

## Data Model

```
EscrowTransaction
├── Id                  int (PK, auto-increment)
├── ClientEmail         string (required) — payer
├── ConsultantEmail     string (required) — payee
├── Amount              decimal (required)
├── ServiceDescription  string (required)
├── Status              string — "Pending" | "Held" | "Released" | "Disputed"
├── ExternalReference   string? — Stripe PaymentIntent ID or ETH tx hash
├── ExternalProvider    string? — "Stripe" | "PayPal" | "Ethereum"
├── DisputeReason       string? — set when Status = "Disputed"
└── CreatedAt           DateTime (UTC)
```

---

## Documentation — MANDATORY

Update `EscrowApp/docs/` when features change. Docs are organized by concern:

```
architecture/overview          Cross-cutting architecture
architecture/payment-strategies Strategy pattern & providers
architecture/event-bus         Domain events & event bus
architecture/api-integration   REST API & Swagger
architecture/stripe-webhooks   Stripe webhook handling
features/hold-funds            Hold funds workflow
features/release-funds         Release funds workflow
features/dispute-funds         Dispute workflow
features/cancel-funds          Cancel escrow workflow
features/client-dashboard      Client dashboard UI
features/consultant-dashboard  Consultant dashboard UI
features/transaction-detail    Transaction detail view
features/landing-page          Landing page components
cross-cutting/hybrid-identity  Identity & auth mapping
cross-cutting/localization     i18n/l10n setup
cross-cutting/testing          Test strategy
audits/security-audit          OWASP audit findings
audits/compliance-audit        Compliance audit log
operations/deployment          Deployment strategy
business/business-model        Revenue model & pricing
```

New features without a matching doc → create under the appropriate concern category.

### Planning Documentation — MANDATORY

Always update **both** planning docs when project status changes:

- `planning/implementation-plan` — Phase status, completion %, what's built vs missing, MVP priorities
- `planning/task-checklist` — Granular task checkboxes (`[x]` / `[ ]`) per phase

**Update triggers:** feature completed, new feature added, test implemented, stub replaced, architecture changed, new handler/component/strategy created. Update the `Last synced with codebase` date in both files.

---

## DI Registration (Program.cs)

When adding new services, register them in `Program.cs` following existing patterns:

```csharp
// Repository
builder.Services.AddScoped<INewRepository, NewRepository>();

// Strategy (new payment provider)
builder.Services.AddScoped<IEscrowPaymentStrategy, NewProviderStrategy>();

// Event handler
// (auto-discovered by MediatR if implementing INotificationHandler<T>)

// New service
builder.Services.AddScoped<INewService, NewService>();
```

MediatR handlers are auto-discovered — no manual registration needed.

---

## Agent Orchestration — MANDATORY

When delegating work to sub-agents (parallel or serial):

1. **ALWAYS present the delegation plan to the user** before spawning any agent.
2. **Use `ask_user`** to show: agent count, agent types, task descriptions, blast radius, estimated tokens.
3. **Wait for explicit approval** — do not assume approval from silence or prior permissions.
4. **Never spawn agents without the user seeing and approving the plan first.**

See `.github/skills/ai/agent-orchestrator/SKILL.md` (Step 3) for the full approval gate workflow.

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for the complete skill loading instructions, categories,
and usage examples. Skills are universal across all models.

**Quick start:** Read `.github/skills/CATALOG.md` to browse all 43 skills across 12 categories.
