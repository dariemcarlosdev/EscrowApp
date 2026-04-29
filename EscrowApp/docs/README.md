# Documentation Index

> **NexTruzt.io EscrowApp** — Comprehensive documentation organized by module and concern for fast navigation.

## Quick Navigation

### 🔐 Authentication Module
All user authentication and identity management:
- [User Login](modules/authentication/user-login/user-login.md) — Sign in with email/password
- [User Registration](modules/authentication/user-registration/user-registration.md) — Create new accounts  
- [ASP.NET Identity Setup](modules/authentication/aspnet-identity-mvp/aspnet-identity-mvp.md) — Identity configuration and MVP approach
- [Hybrid Identity](modules/authentication/hybrid-identity/hybrid-identity.md) — Web2/Web3 identity bridging

### 💰 Escrow Payments Module  
All payment escrow operations and workflows:
- [Hold Funds](modules/escrow-payments/hold-funds/hold-funds.md) — Authorize payment holds via Stripe
- [Release Funds](modules/escrow-payments/release-funds/release-funds.md) — Capture held payments
- [Dispute Funds](modules/escrow-payments/dispute-funds/dispute-funds.md) — Handle payment disputes
- [Cancel Funds](modules/escrow-payments/cancel-funds/cancel-funds.md) — Void payment holds cooperatively
- [Platform Fee](modules/escrow-payments/platform-fee/platform-fee.md) — Fee calculation and collection

### 🖥️ User Interface Module
All UI components, dashboards, and user experiences:
- [Client Dashboard](modules/user-interface/client-dashboard/client-dashboard.md) — Client transaction management
- [Consultant Dashboard](modules/user-interface/consultant-dashboard/consultant-dashboard.md) — Consultant earnings tracking
- [Transaction Detail](modules/user-interface/transaction-detail/transaction-detail.md) — Transaction detail views
- [Landing Page](modules/user-interface/landing-page/landing-page.md) — Marketing page components

### ⚙️ System Module
Cross-cutting system concerns and frameworks:
- [Input Validation](modules/system/input-validation/input-validation.md) — Validation framework and patterns
- [Validation Rules](modules/system/validation-rules/validation-rules.md) — Business validation rules
- [Localization](modules/system/localization/localization.md) — Internationalization (i18n) setup
- [Testing Strategy](modules/system/testing/testing-strategy.md) — Test patterns and frameworks
- [AI Features](modules/system/ai-features/ai-features.md) — AI integration patterns

### 🏗️ Platform
Core platform architecture, operations, and business:
- [Architecture Overview](platform/architecture/overview/overview.md) — System design and patterns
- [Payment Strategies](platform/architecture/payment-strategies/payment-strategies.md) — Strategy pattern implementation
- [Event Bus](platform/architecture/event-bus/event-bus.md) — Domain events and messaging
- [API Integration](platform/architecture/api-integration/api-integration.md) — REST API design
- [Stripe Webhooks](platform/architecture/stripe-webhooks/stripe-webhooks.md) — Webhook handling
- [Deployment](platform/operations/deployment/deployment.md) — Production deployment guide
- [Business Model](platform/business/business-model/strategic-plan.md) — Revenue model and compliance

### 📋 Management
Project planning, auditing, and compliance:
- [Features Inventory](features-inventory.md) — Complete feature implementation status
- [Implementation Plan](planning/implementation-plan.md) — 30-day development roadmap  
- [Task Checklist](planning/task-checklist.md) — Granular execution tracking
- [Security Audit](audits/security-audit/security-audit.md) — OWASP Top 10 compliance
- [Compliance Audit](audits/compliance-audit/compliance-audit.md) — Fintech regulatory review

---

## Documentation Organization Benefits

**Before (Scattered):**
- Auth docs split across `features/user-login/`, `features/user-registration/`, `cross-cutting/authentication/`, `cross-cutting/hybrid-identity/`
- Developers spend minutes hunting for related information across multiple directories

**After (Module-Based):**
- All auth information in `modules/authentication/` 
- All payment information in `modules/escrow-payments/`
- Context discovery reduced from minutes to seconds

**Pattern:** Group by business concern first, then by feature type. Eliminates documentation archaeology.

---

**Last Updated:** 2026-04-16  
**Organization:** Module-based documentation for accelerated context discovery