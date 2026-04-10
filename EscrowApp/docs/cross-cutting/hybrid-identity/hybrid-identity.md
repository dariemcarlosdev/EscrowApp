# Hybrid Identity

> Cross-cutting concern: Actor model and multi-provider identity mapping for Web2/Web3 bridge.

## Overview

The hybrid identity system decouples user identity from any single authentication provider. It supports traditional Web2 authentication (email, OAuth) alongside Web3 wallet-based identity, enabling a future bridge to blockchain-based escrow.

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
- Future: `IClaimsTransformation` to enrich `ClaimsPrincipal` with Actor-based claims
- Future: Wallet signature verification middleware for Web3 authentication

## Related Documentation

- [Architecture Overview](../../architecture/overview/architecture-overview.md) — system design and layer boundaries
- [Payment Strategies](../../architecture/payment-strategies/payment-strategies.md) — how identity maps to payment providers
