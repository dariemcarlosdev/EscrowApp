# EscrowApp Documentation

> Organized by concern. Each folder contains a named `.md` file with feature-level documentation.

## Architecture

Core system design, patterns, and integration boundaries.

| Doc | Topic |
|---|---|
| [architecture/overview](architecture/overview/architecture-overview.md) | System design, pillars, layer boundaries |
| [architecture/payment-strategies](architecture/payment-strategies/payment-strategies.md) | Strategy pattern, ISP interfaces, provider abstraction |
| [architecture/event-bus](architecture/event-bus/event-bus.md) | Domain events, IEventBus, InMemoryEventBus |
| [architecture/api-integration](architecture/api-integration/api-integration.md) | REST API, Swagger, API key authentication |
| [architecture/stripe-webhooks](architecture/stripe-webhooks/stripe-webhooks.md) | Stripe webhook handling and event processing |
| [architecture/stripe-webhooks (MVP)](architecture/stripe-webhooks/minimal-webhook-handler-mvp.md) | Minimal webhook handler for `payment_intent.succeeded` (#7) |

## Features

Individual escrow workflows and UI features.

| Doc | Topic |
|---|---|
| [features/hold-funds](features/hold-funds/hold-funds.md) | Hold funds flow (Stripe manual capture) |
| [features/release-funds](features/release-funds/release-funds.md) | Release funds flow (capture PaymentIntent) |
| [features/dispute-funds](features/dispute-funds/dispute-funds.md) | Dispute flow and resolution |
| [features/cancel-funds](features/cancel-funds/cancel-funds.md) | Cancel escrow and void held funds |
| [features/client-dashboard](features/client-dashboard/client-dashboard.md) | Client-facing transaction dashboard |
| [features/consultant-dashboard](features/consultant-dashboard/consultant-dashboard.md) | Consultant-facing earnings dashboard |
| [features/transaction-detail](features/transaction-detail/transaction-detail.md) | Transaction detail view |
| [features/landing-page](features/landing-page/landing-page.md) | Landing page components and UI |

## Cross-Cutting

Concerns that span multiple layers.

| Doc | Topic |
|---|---|
| [cross-cutting/authentication](cross-cutting/authentication/aspnet-identity-mvp.md) | ASP.NET Core Identity for MVP email/password auth (#3) |
| [cross-cutting/hybrid-identity](cross-cutting/hybrid-identity/hybrid-identity.md) | Actor model, IdentityMapping, Web2/Web3 bridge |
| [cross-cutting/localization](cross-cutting/localization/localization.md) | IStringLocalizer, .resx files, culture switching |
| [cross-cutting/testing](cross-cutting/testing/testing-strategy.md) | Test strategy, xUnit, FluentAssertions |

## Audits

Security and compliance reviews.

| Doc | Topic |
|---|---|
| [audits/security-audit](audits/security-audit/security-audit.md) | OWASP Top 10 audit findings |
| [audits/compliance-audit](audits/compliance-audit.md) | Compliance audit and remediation log |

## Operations

Deployment and infrastructure.

| Doc | Topic |
|---|---|
| [operations/deployment](operations/deployment/deployment.md) | Deployment strategy and environments |
| [operations/deployment (MVP)](operations/deployment/cloud-deployment-steps-mvp.md) | Cloud deployment steps for Azure Container Apps (#8) |

## Business

Business model and monetization.

| Doc | Topic |
|---|---|
| [business/business-model](business/business-model/business-model.md) | Revenue model, pricing, market strategy |

## Planning

Project execution tracking (historical).

| Doc | Topic |
|---|---|
| [planning/task-checklist](planning/task-checklist.md) | Sprint execution checklist |
| [planning/implementation-plan](planning/implementation-plan.md) | 30-day implementation plan |
