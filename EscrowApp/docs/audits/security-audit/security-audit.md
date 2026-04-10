# 10 — Security Audit Report

> **Application:** NexTruzt.io EscrowApp  
> **Stack:** .NET 10, Blazor Server, PostgreSQL, Stripe SDK  
> **Audit Date:** April 6, 2026  
> **Method:** Automated 3-agent parallel analysis (Domain, Infrastructure, Presentation)  
> **Standard:** OWASP Top 10 (2021)

---

## Executive Summary

A comprehensive security audit was performed across all three architectural layers of the
NexTruzt.io EscrowApp using parallel AI-driven analysis agents. Each agent focused on a
specific layer — Domain (models, events, strategies), Infrastructure (data access, auth,
middleware), and Presentation (Blazor components, routing, markup).

### Key Metrics

| Metric | Value |
|--------|-------|
| **Total Findings** | 28 |
| 🔴 Critical | 4 |
| 🟠 High | 8 |
| 🟡 Medium | 10 |
| 🟢 Low | 6 |
| **Layers Analyzed** | 3 (Domain, Infrastructure, Presentation) |
| **Files Scanned** | 58 |
| **OWASP Categories Hit** | A01, A02, A03, A04, A05, A07, A08, A09 |

### Risk Assessment

The application has **critical security gaps** that must be resolved before any production
deployment. The most severe issues are:

1. **Hardcoded secrets in source code** — Stripe API keys, database credentials, and API
   keys are committed in `appsettings.json`. This is the single highest-risk finding.
2. **No route-level authorization** — All pages are publicly accessible. The Blazor
   `Routes.razor` lacks `<AuthorizeRouteView>`, meaning the entire application operates
   in a default-allow mode.
3. **Mass assignment on financial entities** — `EscrowTransaction.Status`, `.Amount`, and
   `.ExternalReference` have public setters, allowing potential tampering through model
   binding.
4. **No state machine validation** — Transaction status transitions are not enforced,
   allowing invalid flows (e.g., Released → Pending).

### Positive Findings

- ✅ EF Core is used consistently — **no raw SQL injection vectors** detected
- ✅ Blazor's built-in encoding prevents most XSS — **no `MarkupString` abuse** found
- ✅ Strategy Pattern properly isolates payment providers — **no cross-provider leakage**
- ✅ Code-behind pattern enforced — **no inline `@code {}` blocks** with sensitive logic

---

## Findings by Layer

### 🔵 Domain Layer (18 findings)

| ID | OWASP | Severity | Finding | File |
|----|-------|----------|---------|------|
| D-001 | A01/A08 | 🔴 Critical | Mass assignment on `Status` — public setter allows external manipulation | `Models/EscrowTransaction.cs` |
| D-002 | A01/A08 | 🔴 Critical | Mass assignment on `ExternalReference` — payment ID tampering | `Models/EscrowTransaction.cs` |
| D-003 | A01/A08 | 🔴 Critical | Mass assignment on `Amount` — financial value manipulation | `Models/EscrowTransaction.cs` |
| D-004 | A04 | 🟠 High | No state machine validation — invalid transitions allowed | `Models/EscrowTransaction.cs` |
| D-005 | A02 | 🟡 Medium | Event payloads leak PII (emails, payment IDs) | `Events/PaymentReceivedEvent.cs` |
| D-006 | A01 | 🟠 High | No authorization/ownership check on entity access | MediatR Handlers |
| D-007 | A04 | 🟡 Medium | InMemoryEventBus not production-safe — no durability or failure handling | `Events/InMemoryEventBus.cs` |
| D-008 | A02 | 🔴 Critical | Stripe API key hardcoded in appsettings (duplicate of I-001) | `appsettings.json` |
| D-009 | A03 | 🟡 Medium | No input validation on `DisputeReason` — log injection possible | `Models/EscrowTransaction.cs` |
| D-010 | A04 | 🟢 Low | Weak idempotency key generation — predictable patterns | Strategy implementations |
| D-011 | A02 | 🟡 Medium | Email addresses stored as plain text — no hashing or encryption | `Models/EscrowTransaction.cs` |
| D-012 | A04 | 🟢 Low | No validation of `ExternalReference` format (Stripe PI ID vs ETH hash) | `Models/EscrowTransaction.cs` |
| D-013 | A09 | 🟡 Medium | No audit logging for transaction state changes | All state transitions |
| D-014 | A04 | 🟢 Low | String-based state machine — should be enum for type safety | `Models/EscrowTransaction.cs` |
| D-015 | A01 | 🟢 Low | Overly permissive API key scope — coarse-grained authorization | Auth configuration |
| D-016 | A02 | 🟡 Medium | No encryption for sensitive fields at rest | `Models/EscrowTransaction.cs` |
| D-017 | A05 | 🟠 High | Hardcoded Stripe `ReturnUrl` with localhost in strategy | `StripePaymentStrategy.cs` |
| D-018 | A09 | 🟢 Low | No request/response logging for payment operations | Strategy implementations |

### 🟢 Infrastructure Layer (7 findings)

| ID | OWASP | Severity | Finding | File |
|----|-------|----------|---------|------|
| I-001 | A02 | 🔴 Critical | Hardcoded secrets — Stripe key, DB creds, API key in committed config | `appsettings.json`, `Program.cs` |
| I-002 | A07 | 🟠 High | API key comparison vulnerable to timing attacks | `Infrastructure/Auth/ApiKeyAuthenticationHandler.cs` |
| I-003 | A01 | 🟠 High | No row-level security — any user can access any transaction | `Data/Repositories/EscrowTransactionRepository.cs` |
| I-004 | A05 | 🟠 High | Swagger exposed in all environments (not gated to Development) | `Program.cs` |
| I-005 | A02 | 🟡 Medium | Exception middleware may leak stack traces in production | `Infrastructure/Middleware/ApiExceptionMiddleware.cs` |
| I-006 | A05 | 🟡 Medium | Missing explicit HTTPS/HSTS enforcement in middleware pipeline | `Program.cs` |
| I-007 | A03 | ✅ Safe | No SQL injection — EF Core parameterized queries throughout | `Data/Repositories/` |

### 🟣 Presentation Layer (10 findings)

| ID | OWASP | Severity | Finding | File |
|----|-------|----------|---------|------|
| P-001 | A01 | 🔴 Critical | No `<AuthorizeRouteView>` — all routes publicly accessible | `Components/Routes.razor` |
| P-002 | A04 | 🟠 High | Client-side only email validation — no server-side enforcement | `Components/Pages/HeroSection.razor` |
| P-003 | A01 | 🟠 High | Open redirect via unvalidated `redirectUri` parameter | `Components/Pages/NavBar.razor.cs` |
| P-004 | A09 | 🟡 Medium | Error page exposes Request ID and dev-mode instructions | `Components/Pages/Error.razor` |
| P-005 | A08 | 🟡 Medium | Missing CSRF/antiforgery protection on forms | `Components/Pages/HeroSection.razor` |
| P-006 | A06 | 🟢 Low | JS interop lacks error boundaries in ReconnectModal | `Components/Layout/ReconnectModal.razor` |
| P-007 | A05 | 🟡 Medium | No CSP, SRI, or security meta headers in App.razor | `Components/App.razor` |
| P-008 | A01 | 🔴 Critical | Default-allow routing model — fintech app requires default-deny | `Components/Routes.razor` |
| P-009 | A08 | 🟡 Medium | Culture cookie missing HttpOnly, Secure, SameSite flags | `Program.cs` |
| P-010 | A07 | 🟢 Low | No rate limiting on form submissions | `Components/Pages/HeroSection.razor` |

---

## Implementation Checklist

### 🔴 Week 1 — Critical (Must fix before any deployment)

- [ ] **SEC-001: Remove all hardcoded secrets from source** (I-001, D-008)
  - [ ] Run `dotnet user-secrets init` in the EscrowApp project
  - [ ] Move Stripe SecretKey to user secrets: `dotnet user-secrets set "Stripe:SecretKey" "sk_test_xxx"`
  - [ ] Move DB connection string to user secrets: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=..."`
  - [ ] Move API key to user secrets: `dotnet user-secrets set "ApiSettings:Key" "ntzt_dev_xxx"`
  - [ ] Replace values in `appsettings.json` with empty strings or placeholders
  - [ ] Scrub git history: `git filter-repo --invert-paths --path appsettings.json`
  - [ ] Rotate ALL compromised keys (Stripe, DB password, API key)
  - [ ] For production: configure Azure Key Vault or equivalent secrets manager

- [ ] **SEC-002: Enable route-level authorization** (P-001, P-008)
  - [ ] Replace `<RouteView>` with `<AuthorizeRouteView>` in `Routes.razor`
  - [ ] Add `<NotAuthorized>` redirect to login page
  - [ ] Add `[Authorize]` attribute to all protected page components
  - [ ] Add `[AllowAnonymous]` only to public pages (Home, Error, NotFound)
  - [ ] Verify default-deny: unauthenticated users see login, not blank pages

- [ ] **SEC-003: Protect EscrowTransaction from mass assignment** (D-001, D-002, D-003)
  - [ ] Change `Status` property to `private set` with a public transition method
  - [ ] Change `Amount` property to `init` (set once at creation, never modified)
  - [ ] Change `ExternalReference` property to `private set` with an internal setter method
  - [ ] Create DTOs for all external input — never bind directly to domain entities
  - [ ] Add guard clauses in the constructor for required fields

### 🟠 Week 2 — High (Fix this sprint)

- [ ] **SEC-004: Implement state machine for EscrowTransaction** (D-004, D-014)
  - [ ] Create `EscrowStatus` enum: `Pending`, `Held`, `Released`, `Disputed`, `Cancelled`
  - [ ] Add `TransitionTo(EscrowStatus newStatus)` method with valid transition map
  - [ ] Throw `InvalidOperationException` on invalid transitions
  - [ ] Add unit tests for all valid and invalid transition paths

- [ ] **SEC-005: Fix API key timing attack** (I-002)
  - [ ] Replace `==` string comparison with `CryptographicOperations.FixedTimeEquals()`
  - [ ] Convert both key values to fixed-length byte arrays before comparison
  - [ ] Add rate limiting on authentication failures (e.g., 5 failures per minute per IP)

- [ ] **SEC-006: Add row-level security to repository** (I-003, D-006)
  - [ ] Create `ICurrentUserService` interface returning the authenticated user's identity
  - [ ] Inject `ICurrentUserService` into `EscrowTransactionRepository`
  - [ ] Filter all queries by `ClientEmail` or `ConsultantEmail` matching current user
  - [ ] Add ownership validation in MediatR handlers before any state change
  - [ ] Add integration tests verifying cross-user access is denied

- [ ] **SEC-007: Gate Swagger to Development only** (I-004)
  - [ ] Wrap `app.UseSwagger()` and `app.UseSwaggerUI()` in `if (app.Environment.IsDevelopment())`
  - [ ] Verify Swagger is not accessible when `ASPNETCORE_ENVIRONMENT=Production`

- [ ] **SEC-008: Validate redirect URIs** (P-003)
  - [ ] Create an allow-list of valid redirect paths (local paths only)
  - [ ] Validate `redirectUri` starts with `/` and does not contain `://`
  - [ ] Reject absolute URLs and external domains
  - [ ] Default to `/` if validation fails

- [ ] **SEC-009: Add server-side email validation** (P-002)
  - [ ] Add FluentValidation validator for the email submission model
  - [ ] Validate email format, length, and domain on the server
  - [ ] Return structured validation errors to the Blazor component

- [ ] **SEC-010: Fix hardcoded ReturnUrl** (D-017)
  - [ ] Move Stripe ReturnUrl to configuration (`appsettings.json` or environment variable)
  - [ ] Use `IOptions<StripeSettings>` to inject the return URL
  - [ ] Ensure production value points to the real domain, not localhost

### 🟡 Week 3 — Medium (Fix next sprint)

- [ ] **SEC-011: Sanitize event payloads** (D-005)
  - [ ] Remove or hash PII (emails) from domain event payloads
  - [ ] Use transaction IDs instead of user emails in events
  - [ ] Ensure event handlers never log raw PII

- [ ] **SEC-012: Add input validation for DisputeReason** (D-009)
  - [ ] Add max length constraint (e.g., 2000 characters)
  - [ ] Sanitize for HTML/script injection before persistence
  - [ ] Add FluentValidation rule in the Dispute command validator

- [ ] **SEC-013: Harden exception middleware** (I-005)
  - [ ] Return generic error message in Production (`"An unexpected error occurred"`)
  - [ ] Include stack traces only when `IsDevelopment()` is true
  - [ ] Log full exception server-side with correlation ID
  - [ ] Return correlation ID to client for support reference

- [ ] **SEC-014: Enforce HTTPS and HSTS** (I-006)
  - [ ] Add `app.UseHttpsRedirection()` to middleware pipeline
  - [ ] Add `app.UseHsts()` for production environments
  - [ ] Configure HSTS max-age to at least 1 year (31536000 seconds)

- [ ] **SEC-015: Add security headers** (P-007)
  - [ ] Add Content-Security-Policy meta tag to `App.razor`
  - [ ] Add Subresource Integrity (SRI) hashes for CDN resources (Bootstrap, etc.)
  - [ ] Add `X-Content-Type-Options: nosniff` header
  - [ ] Add `X-Frame-Options: DENY` header
  - [ ] Add `Referrer-Policy: strict-origin-when-cross-origin` header

- [ ] **SEC-016: Add CSRF/antiforgery protection** (P-005)
  - [ ] Replace plain HTML forms with `<EditForm>` components
  - [ ] Add `@rendermode InteractiveServer` where forms are used
  - [ ] Add `[ValidateAntiForgeryToken]` on API endpoints
  - [ ] Verify Blazor's built-in antiforgery is active for interactive components

- [ ] **SEC-017: Secure culture cookie** (P-009)
  - [ ] Set `HttpOnly = true` on culture cookie options
  - [ ] Set `Secure = true` (HTTPS only)
  - [ ] Set `SameSite = SameSiteMode.Lax` (or Strict)
  - [ ] Set reasonable expiration (e.g., 30 days)

- [ ] **SEC-018: Clean up Error page** (P-004)
  - [ ] Remove `RequestId` display from production Error page
  - [ ] Remove development-mode switching instructions
  - [ ] Show user-friendly error message with support contact

- [ ] **SEC-019: Add audit logging for state transitions** (D-013)
  - [ ] Log all `Status` changes with: old status, new status, user, timestamp, correlation ID
  - [ ] Use structured logging (Serilog or built-in)
  - [ ] Never log PII — use hashed identifiers

- [ ] **SEC-020: Encrypt sensitive fields at rest** (D-011, D-016)
  - [ ] Evaluate EF Core value converters for encrypting `ClientEmail`, `ConsultantEmail`
  - [ ] Use AES-256 with keys from Key Vault
  - [ ] Maintain searchability via hashed index columns if needed

### 🟢 Week 4 — Low (Backlog / hardening)

- [ ] **SEC-021: Strengthen idempotency keys** (D-010)
  - [ ] Use `Guid.NewGuid()` or cryptographic random for idempotency keys
  - [ ] Never derive keys from predictable values (timestamp + ID)

- [ ] **SEC-022: Validate ExternalReference format** (D-012)
  - [ ] Add regex validation for Stripe PaymentIntent ID format (`pi_xxx`)
  - [ ] Add regex validation for Ethereum transaction hash format (`0x[a-f0-9]{64}`)

- [ ] **SEC-023: Add JS interop error boundaries** (P-006)
  - [ ] Wrap `IJSRuntime` calls in try-catch in ReconnectModal
  - [ ] Dispose `IJSObjectReference` in `IAsyncDisposable`

- [ ] **SEC-024: Add rate limiting** (P-010)
  - [ ] Add `Microsoft.AspNetCore.RateLimiting` middleware
  - [ ] Configure per-IP rate limits on form submission endpoints
  - [ ] Return 429 Too Many Requests with Retry-After header

- [ ] **SEC-025: Add payment operation logging** (D-018)
  - [ ] Log all Stripe API calls with correlation ID (no secrets)
  - [ ] Log response status codes and timing
  - [ ] Alert on repeated failures (circuit breaker pattern)

- [ ] **SEC-026: Replace InMemoryEventBus for production** (D-007)
  - [ ] Evaluate durable alternatives: MassTransit, Azure Service Bus, RabbitMQ
  - [ ] Implement dead-letter queue for failed event handlers
  - [ ] Add retry policies on event handlers

---

## OWASP Coverage Matrix

| OWASP Category | Findings | Top Priority |
|----------------|----------|--------------|
| **A01: Broken Access Control** | P-001, P-008, I-003, D-006, P-003, D-015 | 🔴 SEC-002, SEC-006 |
| **A02: Cryptographic Failures** | I-001, D-008, D-005, D-011, D-016 | 🔴 SEC-001 |
| **A03: Injection** | D-009, I-007 ✅ | 🟡 SEC-012 |
| **A04: Insecure Design** | D-004, D-007, D-010, D-012, D-014, P-002 | 🟠 SEC-004 |
| **A05: Security Misconfiguration** | I-004, I-006, D-017, P-007 | 🟠 SEC-007, SEC-014 |
| **A06: Vulnerable Components** | P-006 | 🟢 SEC-023 |
| **A07: Auth Failures** | I-002, P-010 | 🟠 SEC-005 |
| **A08: CSRF / Forgery** | P-005, P-009 | 🟡 SEC-016, SEC-017 |
| **A09: Logging Failures** | D-013, D-018, P-004 | 🟡 SEC-019 |
| **A10: SSRF** | — | ✅ No findings |

---

## Progress Tracking

| Phase | Items | Status |
|-------|-------|--------|
| Week 1 — Critical | SEC-001, SEC-002, SEC-003 | ⬜ Not Started |
| Week 2 — High | SEC-004 through SEC-010 | ⬜ Not Started |
| Week 3 — Medium | SEC-011 through SEC-020 | ⬜ Not Started |
| Week 4 — Low | SEC-021 through SEC-026 | ⬜ Not Started |

---

*Generated by NexTruzt.io AI Security Audit Fleet — 3 parallel agents, OWASP Top 10 methodology.*
