# OWASP Top 10 Security — Fintech Rules

> This is a fintech platform handling real money. Security-first on every change.

## OWASP Checklist

| # | Category | Requirement |
|---|----------|-------------|
| A01 | Broken Access Control | `[Authorize]` on every endpoint. Policy-based auth. Default deny-all. Never rely on UI hiding. |
| A02 | Cryptographic Failures | Secrets via Key Vault or env vars. Never in code/config. Encrypt PII at rest. HTTPS enforced. |
| A03 | Injection | Parameterized queries only (EF Core). No SQL string concatenation. FluentValidation on all input. |
| A04 | Insecure Design | Strategy Pattern enforces payment boundaries. Threat model critical flows. |
| A05 | Security Misconfiguration | HTTPS + HSTS. Antiforgery tokens. Swagger only in Development. Secure headers (CSP, X-Frame-Options). |
| A06 | Vulnerable Components | Keep NuGet packages updated. Monitor for CVEs. |
| A07 | Auth Failures | API key via `X-Api-Key` header validated every request. No custom auth — use Entra ID or ASP.NET Identity. |
| A08 | Data Integrity | Safe deserialization. Verify pipeline integrity. |
| A09 | Logging Failures | Structured logging with correlation IDs. **Never log PII, tokens, secrets, or connection strings.** |
| A10 | SSRF | Validate external URLs. Use allowlists for outbound calls. |

## Fintech-Specific Rules

- **Never store raw card numbers** — delegate to Stripe, store only PaymentIntent IDs
- **Idempotency keys** on every payment mutation (hold, capture, refund)
- **Audit trail** — every state transition emits domain events for regulatory traceability
- **Manual capture only** — Stripe PaymentIntents use `capture_method: manual`
- **Dispute blocks release** — disputed transactions cannot be released without resolution

## Authorization Pattern

```csharp
// ✅ Policy-based — centralized, testable
[Authorize(Policy = "CanReleaseFunds")]

// ❌ Role strings scattered — never do this
[Authorize(Roles = "Admin,Officer")]
```

## Logging Safety

```csharp
// ✅ Log correlation IDs only
_logger.LogInformation("Payment processed for {TransactionId}", transaction.Id);

// ❌ NEVER log PII or secrets
_logger.LogInformation("Payment for {Email} with key {ApiKey}", email, key);
```
