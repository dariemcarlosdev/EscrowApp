# Authentication — Cross-Cutting Concern

> User authentication and authorization documentation for NexTruzt.io EscrowApp.

## Documents

| Document | Concern | Status |
|----------|---------|--------|
| `aspnet-identity-mvp.md` | ASP.NET Core Identity implementation for MVP (#3) | 📋 Design doc for #3 implementation |

## Quick Links

- **MVP Authentication:** [ASP.NET Core Identity](aspnet-identity-mvp.md)
  - Email/password registration and login
  - Database schema (ApplicationUser, Identity tables)
  - DI configuration and Blazor integration
  - Security guardrails (password hashing, CSRF, account lockout)
  - Testing patterns (unit + integration)

## Related Documents

- [Hybrid Identity](../hybrid-identity/hybrid-identity.md) — Actor model and Web2/Web3 bridge
- [Deployment](../../operations/deployment/deployment.md) — Production auth configuration
- [OWASP Security](../../audits/security-audit/owasp-audit.md) — Security audit findings
