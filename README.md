# NexTruzt.io — Secure Payment Holding Platform

![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![Tests](https://img.shields.io/badge/tests-132%2F132-brightgreen) ![.NET](https://img.shields.io/badge/.NET-10-blue)

A modern fintech platform providing **secure payment holding** for independent consultants and their clients, powered by Stripe Connect delayed payouts with a planned Web3/Ethereum bridge.

## 🎯 What is NexTruzt.io?

**Pay with confidence. Get paid with certainty.**

NexTruzt.io eliminates payment risk for both parties:
- **Clients** hold funds securely until work is delivered ✅
- **Consultants** know payment is locked in before starting work ✅
- **Disputes** are handled transparently with clear resolution paths ✅

## 🚀 Key Features

### Secure Payment Holding
- **Authorization Hold:** Clients initiate payments via Stripe (funds not captured yet)
- **Work Completion:** Consultant delivers work
- **Fund Release:** Client releases payment when satisfied
- **Payout to Consultant:** Funds transfer to consultant's bank account

### Payment Management
- **Multiple Payment States:** Pending → Held → Released | Disputed
- **Dispute Resolution:** Transparent process for payment disagreements
- **Real-Time Webhooks:** Instant processing of Stripe payment events
- **Idempotent Operations:** Safe retry logic for all payment operations

### User Authentication
- **Secure Login/Register:** ASP.NET Identity with 2FA-ready architecture
- **Consultant Accounts:** Full identity management
- **Client Accounts:** Streamlined checkout experience
- **Hybrid Identity Bridge:** Web2/Web3 ready for future Ethereum integration

## 📊 Project Status

### ✅ Completed Tracks

| Track | Progress | Details |
|-------|----------|---------|
| **Track B: Authentication** | 100% ✅ | 14 tasks • 122 tests • Login, register, identity infrastructure |
| **Track C: Stripe Webhooks** | 100% ✅ | 13 tasks • 15 tests • Event handling, signature verification |

### 🧪 Test Coverage
- **Total Tests:** 132/132 passing ✅
- **Build Status:** 0 errors, 0 warnings
- **Unit Tests:** 117 | Integration Tests: 15 | Skipped: 1 (optional)

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Blazor Server (interactive SSR) with Bootstrap 5 |
| **Backend** | .NET 10, ASP.NET Core, Clean Architecture + CQRS |
| **Database** | PostgreSQL with EF Core, Npgsql driver |
| **Payment Processing** | Stripe SDK, manual capture flow, webhooks |
| **Business Logic** | MediatR (command/query separation), FluentValidation |
| **Internationalization** | IStringLocalizer (en-US, es-MX) |

## 🏛️ Architecture

Clean Architecture with CQRS via MediatR:

```
┌─────────────────────────────────────────────┐
│  UI Layer (Components/)                     │
│  Blazor pages, layouts, shared components   │
├─────────────────────────────────────────────┤
│  Application Layer (Features/)              │
│  CQRS handlers, validators, business logic  │
├─────────────────────────────────────────────┤
│  Domain Layer (Models/ + Events/)           │
│  Entities, value objects, domain events     │
├─────────────────────────────────────────────┤
│  Infrastructure Layer (Data/ + Services/)   │
│  EF Core, Stripe integration, repositories  │
└─────────────────────────────────────────────┘
```

### Key Design Patterns

- **Strategy Pattern** — Payment provider abstraction (Stripe, future PayPal/Ethereum)
- **CQRS** — Separate command (write) and query (read) paths
- **Domain Events** — `PaymentReceivedEvent`, `DisputeRaisedEvent` via `IEventBus`
- **Repository Pattern** — Data access abstraction via `IEscrowTransactionRepository`
- **Code-Behind** — All Blazor components use `.razor` + `.razor.cs` + `.razor.css`
- **Idempotency Keys** — All payment operations safe for retry

## 📁 Project Structure

```
EscrowApp/
├── Components/              Blazor UI (pages, layouts, shared)
├── Features/                CQRS handlers (commands, queries)
├── Models/                  Domain entities (Payment, Actor, etc.)
├── Events/                  Domain events & event bus
├── Data/                    EF Core DbContext, repositories
├── Services/                Business logic, payment strategies
├── Infrastructure/          Stripe integration, webhooks, auth
├── Migrations/              EF Core database migrations
├── Resources/               i18n strings (.resx files)
├── appsettings*.json        Environment configuration
└── docs/                    Architecture & feature docs

EscrowApp.Tests/
├── Features/                Handler tests
├── Infrastructure/          Webhook & auth tests
├── Models/                  Domain entity tests
└── bin/                     Test output
```

## 🔐 Security

### OWASP-First Approach
- ✅ **Broken Access Control:** `[Authorize]` on all endpoints, policy-based (default deny)
- ✅ **Cryptographic Failures:** Secrets via env vars/Key Vault, never in code
- ✅ **Injection:** Parameterized queries (EF Core), no SQL concatenation
- ✅ **Insecure Design:** Strategy pattern enforces payment provider boundaries
- ✅ **Security Misconfiguration:** HTTPS enforced, HSTS enabled, antiforgery tokens
- ✅ **Vulnerable Components:** NuGet packages kept current, CVE monitoring
- ✅ **Auth Failures:** API key validation, signature verification on webhooks
- ✅ **Logging Failures:** Structured logging, no PII/tokens/secrets in logs

### Payment Security
- **Stripe Signature Verification:** HMAC-SHA256, constant-time comparison
- **Manual Capture Flow:** Funds authorized but not captured until explicitly released
- **Idempotency Keys:** All operations safe for retry (no duplicate charges)
- **Audit Trail:** Every state transition emits domain events for compliance traceability

## 🚦 Getting Started

### Prerequisites
- .NET 10 SDK
- PostgreSQL 14+
- Stripe API keys (test and live)

### Local Setup

```bash
# Clone repository
git clone <repo-url>
cd EscrowApp

# Install dependencies
dotnet restore

# Configure local database
# Update appsettings.Development.json with your PostgreSQL connection string

# Apply migrations
dotnet ef database update

# Run tests
dotnet test

# Start development server
dotnet run
```

### Environment Configuration

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=escrowapp;User Id=postgres;Password=..."
  },
  "Stripe": {
    "ApiKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "Webhook": {
      "EndpointSecret": "whsec_test_..."
    }
  }
}
```

## 📚 Documentation

- **[Architecture Overview](EscrowApp/docs/platform/architecture/overview/README.md)** — System design, layers, dependencies
- **[Payment Strategies](EscrowApp/docs/platform/architecture/payment-strategies/README.md)** — Provider abstraction, Stripe integration
- **[Stripe Webhooks](EscrowApp/docs/platform/architecture/stripe-webhooks/stripe-webhooks.md)** — Event handling, signature verification
- **[Feature Docs](EscrowApp/docs/platform/features/)** — Hold funds, release funds, dispute handling
- **[Implementation Plan](EscrowApp/docs/planning/implementation-plan.md)** — Track status, milestones, next steps
- **[Task Checklist](EscrowApp/docs/planning/task-checklist.md)** — Granular task tracking

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter ClassName=PaymentIntentEventHandlerTests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Show test summary
dotnet test --logger "console;verbosity=quiet"
```

## 🔄 CI/CD

Automated testing and build validation on every push:
- Build verification (.NET 10)
- Test suite execution (132 tests)
- Code quality checks
- Security scanning

## 💰 Business Model

### Revenue Streams

| Stream | Rate | Trigger |
|--------|------|---------|
| **Platform Fee** | 1.5% | Every held payment |
| **Express Payout** | 0.5% (min $1) | Consultant opt-in for next-day release |
| **Instant Payout** | 1.5% (min $2) | Consultant opt-in for 30-min release |

### Example Transaction
```
Client holds $5,000 for project work
├── Platform fee (1.5%):        $75.00  → NexTruzt.io
├── Stripe processing (2.9%):   $145.00 → Stripe
└── Consultant receives:        $4,780.00
```

## ⚖️ Legal & Compliance

### Important Notice
**NexTruzt.io is NOT a licensed escrow agent or money transmitter.**

The platform provides **secure payment holding** via Stripe Connect—a payment platform model, not a traditional escrow service. All legal structures, disclaimers, and compliance measures must be reviewed by a fintech attorney before production launch.

### Pre-Launch Compliance Checklist
- [ ] Fintech attorney engagement and legal opinion
- [ ] Money transmitter licensing assessment (state-by-state)
- [ ] Terms of Service reviewed and approved
- [ ] Privacy Policy (GDPR/CCPA compliant)
- [ ] Stripe Connect compliance verification
- [ ] User-facing terminology audit (no "escrow" language)

See [Strategic Pre-Launch Plan](EscrowApp/docs/platform/business/business-model/strategic-plan.md) for full compliance roadmap.

## 🔮 Future Roadmap

### v1.0 MVP
✅ Authentication, payment holding, webhooks (CURRENT)

### v1.1
- Consultant & client dashboards
- Transaction history & analytics
- Dispute resolution UI
- Email notifications

### v1.2
- Express payout feature
- Instant payout support
- Multi-currency handling
- Advanced reporting

### v2.0
- Web3/Ethereum bridge
- Decentralized dispute resolution
- DAO governance

## 📞 Support

For questions or issues:
1. Check the [documentation](EscrowApp/docs/) first
2. Review the [task checklist](EscrowApp/docs/planning/task-checklist.md) for known items
3. Open an issue on GitHub

## 📝 License

[Choose your license - MIT, Apache 2.0, etc.]

## 👥 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Built with ❤️ for consultants and their clients.**

Last updated: 2026-04-29 | Track B & C: 100% Complete ✅
