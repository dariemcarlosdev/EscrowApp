# Hybrid Identity

> Cross-cutting concern: Actor model and multi-provider identity mapping for Web2/Web3 bridge.

## Overview

The hybrid identity system decouples user identity from any single authentication provider. It supports traditional Web2 authentication (email, OAuth) alongside Web3 wallet-based identity, enabling a future bridge to blockchain-based escrow.

## User Stories

These stories describe the Actor / IdentityMapping abstraction that decouples user identity from a single authentication provider. They are mostly platform-internal — engineering, admin, and compliance personas — because the hybrid identity is invisible to end users.

### Story 1 — Provider-agnostic user identity

**As a** Developer, **I want** every authenticated user to map to a single provider-agnostic `Actor`, **so that** business logic in handlers and reports does not depend on whether the user authenticated via email, OAuth, or a Web3 wallet.

**Acceptance Criteria:**

- [ ] exactly one Actor row exists for the user
- [ ] exactly one IdentityMapping row exists with Provider="Email" and ExternalId="alice@example.com"
- [ ] a second IdentityMapping is created with Provider="MetaMask" and ExternalId equal to the wallet address
- [ ] both mappings reference the same ActorId

```gherkin
Feature: Actor as the canonical identity
  Scenario: Email user maps to a single Actor
    Given a user registers with email "alice@example.com"
    When the registration handler completes
    Then exactly one Actor row exists for the user
    And exactly one IdentityMapping row exists with Provider="Email" and ExternalId="alice@example.com"

  Scenario: Same Actor with multiple providers
    Given an Actor exists with one IdentityMapping for Provider="Email"
    When the user later links a MetaMask wallet
    Then a second IdentityMapping is created with Provider="MetaMask" and ExternalId equal to the wallet address
    And both mappings reference the same ActorId
```

### Story 2 — Web2-to-Web3 bridge readiness

**As a** Platform Admin, **I want** users to remain identifiable when they later add a wallet address, **so that** their transaction history is preserved across the bridge from Web2 to Web3 settlement.

**Acceptance Criteria:**

- [ ] Actor.WalletAddress is set to the verified address
- [ ] all 3 historical transactions remain attributed to the same Actor

```gherkin
Feature: Wallet linking preserves history
  Scenario: Wallet is linked after several transactions
    Given an Actor with 3 completed transactions and WalletAddress = NULL
    When the user successfully links a MetaMask wallet
    Then Actor.WalletAddress is set to the verified address
    And all 3 historical transactions remain attributed to the same Actor
```

### Story 3 — Multi-provider login does not duplicate identity

**As a** Compliance Officer, **I want** sign-ins from any linked provider to resolve to a single internal Actor, **so that** KYC, audit trails, and counterparty checks reference a single unique person rather than duplicate accounts.

**Acceptance Criteria:**

- [ ] the resolved Actor is the same one returned for the Email mapping
- [ ] no new Actor row is created

```gherkin
Feature: One person, one Actor
  Scenario: Sign-in via secondary provider
    Given an Actor with IdentityMappings for "Email" and "Google" pointing to the same person
    When the user signs in via Google
    Then the resolved Actor is the same one returned for the Email mapping
    And no new Actor row is created
```


## Domain Model

### Actor

The `Actor` entity is a provider-agnostic user identity. It represents a participant in the escrow system without binding to a specific authentication mechanism.

```
Actor
├── Id              int (PK, auto-increment)
├── DisplayName     string (required) — human-readable identity
├── WalletAddress   string? — Web3-ready, null until a wallet is linked
└── CreatedAt       DateTime (UTC)
```

**Key design decisions:**
- `WalletAddress` is nullable — supports pure Web2 users who haven't linked a wallet
- `DisplayName` is required — every actor must have a human-readable identity
- No email field on Actor — email is stored as an `IdentityMapping` with `Provider = "Email"`

### IdentityMapping

Maps an `Actor` to one or more external identity providers. A single user can authenticate via multiple providers.

```
IdentityMapping
├── Id              int (PK, auto-increment)
├── ActorId         int (FK → Actor.Id, required)
├── Provider        string (required) — "Email", "Google", "MetaMask", "WalletConnect"
├── ExternalId      string (required) — email address, OAuth sub claim, or wallet address
└── Actor           navigation property
```

**Relationship:** One Actor → Many IdentityMappings (one per provider)

## Supported Providers

| Provider | ExternalId Format | Authentication Flow |
|---|---|---|
| `Email` | Email address | ASP.NET Core Identity / Magic link |
| `Google` | OAuth `sub` claim | OpenID Connect |
| `MetaMask` | Ethereum address (`0x...`) | Wallet signature verification |
| `WalletConnect` | Ethereum address (`0x...`) | WalletConnect protocol |

## Web2 → Web3 Bridge Pattern

```
Web2 User                    Web3 User
    │                            │
    ▼                            ▼
IdentityMapping              IdentityMapping
(Provider: "Email")          (Provider: "MetaMask")
(ExternalId: "user@x.com")  (ExternalId: "0xABC...")
    │                            │
    └──────────┬─────────────────┘
               ▼
            Actor
    (DisplayName: "John Doe")
    (WalletAddress: "0xABC...")
```

A user who starts with email authentication can later link a MetaMask wallet. Both mappings point to the same `Actor`, enabling seamless transition between Web2 and Web3 escrow flows.

## Data Access

- `Actor` and `IdentityMapping` are registered as DbSets in `EscrowDbContext`
- A unique index exists on `(Provider, ExternalId)` to prevent duplicate mappings
- Actors are created during transaction setup and associated with `EscrowTransaction` participants

## Infrastructure Integration

- `Infrastructure/Auth/ApiKeyAuthenticationHandler` handles API key-based authentication
  - Uses **timing-safe comparison** via `CryptographicOperations.FixedTimeEquals()` to prevent side-channel attacks
  - Reads API keys from configuration via strongly-typed `ApiKeySettings` / `ApiKeyConfig` options pattern
  - Issues claims on successful authentication: `NameIdentifier`, `Name`, `api_client_id`, `scope` (`escrow:read escrow:write`)
- Future: `IClaimsTransformation` to enrich `ClaimsPrincipal` with Actor-based claims
- Future: Wallet signature verification middleware for Web3 authentication

## Related Documentation

- [Architecture Overview](../../../architecture/overview/architecture-overview.md) — system design and layer boundaries
- [Payment Strategies](../../../architecture/payment-strategies/payment-strategies.md) — how identity maps to payment providers
