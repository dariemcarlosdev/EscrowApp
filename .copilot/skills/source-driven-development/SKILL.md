---
name: source-driven-development
description: "Verify API signatures, methods, and behavior against official documentation before implementing. Use when integrating external libraries, frameworks, or unfamiliar APIs to prevent hallucinated code."
---

# Source-Driven Development

> Validate every API call against authoritative sources before writing code. Prevent hallucinated methods, outdated patterns, and broken integrations.

## When to Use

- Integrating a third-party library (Stripe SDK, Azure SDK, NuGet packages)
- Using a framework API you're not 100% certain about (EF Core, Blazor, MediatR)
- Implementing webhook handlers or external API contracts
- Upgrading to a new version of a dependency (.NET 10, EF Core 9)
- User reports "this doesn't work" on code that "should" work
- Implementing code from a tutorial, blog post, or Stack Overflow answer

**When NOT to use:**
- Working with domain models you own (EscrowTransaction, Actor)
- Refactoring internal code within the same project
- Writing business logic that doesn't call external APIs

## Core Workflow

### 1. Identify External Dependency
- ✅ **Checkpoint:** API, library, or framework method identified
- Note the package name, version, and class/method being called
- Distinguish between project code (trustworthy) and external code (verify)

**Examples of external dependencies to verify:**
- Stripe SDK: `PaymentIntentService.CreateAsync()`
- EF Core: `DbContext.SaveChangesAsync()`
- Blazor: `OnAfterRenderAsync()` lifecycle method
- MediatR: `IRequestHandler<TRequest, TResponse>`
- ASP.NET: `[Authorize(Policy = "...")]`

### 2. Find Official Documentation
- ✅ **Checkpoint:** Official docs located (not blog posts or Stack Overflow)
- Go to the source of truth — official docs, API reference, or GitHub repo

**Trust hierarchy:**

| Source | Trust Level | Action |
|---|---|---|
| **Official Microsoft Learn** (learn.microsoft.com) | High | Use directly — this is authoritative for .NET, EF Core, Blazor, ASP.NET |
| **Official vendor docs** (Stripe docs, NuGet package README) | High | Use directly — vendor defines the contract |
| **GitHub source code** (official repo) | High | Read method signatures, XML docs, tests |
| **API reference / IntelliSense XML docs** | High | Verify parameter types, return types, exceptions |
| **Official samples** (microsoft/samples, vendor examples) | Medium-High | Good for patterns, verify API calls match current version |
| **Blog posts by library authors** | Medium | Cross-reference with official docs — may be outdated |
| **Third-party tutorials** (Pluralsight, blog posts) | Medium | Verify EVERY API call against official docs |
| **Stack Overflow answers** | Low | Treat as hypothesis — verify before using |
| **AI training data / model memory** | Lowest | ALWAYS verify — models have training cutoff dates and hallucinate APIs |

**Quick links for NexTruzt.io:**
- .NET 10: https://learn.microsoft.com/en-us/dotnet/
- EF Core 9: https://learn.microsoft.com/en-us/ef/core/
- Blazor: https://learn.microsoft.com/en-us/aspnet/core/blazor/
- Stripe .NET SDK: https://stripe.com/docs/api?lang=dotnet
- MediatR: https://github.com/jbogard/MediatR/wiki

### 3. Verify API Signature
- ✅ **Checkpoint:** Method exists with correct parameters, return type, and behavior
- Check method name (exact spelling, casing)
- Check parameter order, types, optionality
- Check return type and nullability
- Check if method is async (returns `Task` or `Task<T>`)
- Check exceptions thrown (XML docs or API reference)

**Example verification (Stripe):**

Claim to verify:
```csharp
var paymentIntent = await stripeClient.PaymentIntents.CreateAsync(
    new PaymentIntentCreateOptions
    {
        Amount = 1000,
        Currency = "usd",
        CaptureMethod = "manual"
    });
```

**Verify against Stripe docs:**
1. Go to https://stripe.com/docs/api/payment_intents/create
2. Confirm `Amount` is long (cents), `Currency` is string (ISO code)
3. Confirm `CaptureMethod` accepts `"manual"` | `"automatic"`
4. Confirm return type is `PaymentIntent`, not `Task<PaymentIntent>`
5. Verify SDK async wrapper: check `StripeClient` source or IntelliSense

✅ **Verified** — API matches docs.

### 4. Cross-Reference with Existing Codebase
- ✅ **Checkpoint:** Found 1+ examples of this API being used correctly in project
- Search project for existing usage: `grep -r "PaymentIntentService" --include="*.cs"`
- Check how others called this API (parameters, error handling, cancellation tokens)
- Match the pattern unless there's a reason to deviate

**Example:**
```bash
grep -r "SaveChangesAsync" EscrowApp/Data/ --include="*.cs"
```

Found pattern:
```csharp
await _context.SaveChangesAsync(cancellationToken);
```

✅ **Pattern confirmed** — always pass `CancellationToken`.

### 5. Check Version Compatibility
- ✅ **Checkpoint:** API exists in the version you're using
- Verify package version in project: `dotnet list package`
- Check if API was added/changed in recent versions (release notes)
- Confirm breaking changes between versions

**Example (EF Core 8 → 9 migration):**
```powershell
dotnet list package | Select-String "EntityFrameworkCore"
```

Output:
```
Microsoft.EntityFrameworkCore.Design    9.0.0
```

Check EF Core 9 release notes for breaking changes:
- ✅ Confirmed: `SaveChangesAsync()` signature unchanged
- ⚠️ Warning: `UseSnakeCaseNamingConvention()` now in separate NuGet package

### 6. Implement with Verified API
- ✅ **Checkpoint:** Code uses exact API signature from official docs
- Copy method signature from docs/IntelliSense, don't rely on memory
- Use named parameters for clarity (e.g., `amount: 1000, currency: "usd"`)
- Handle return types correctly (check for nullability)
- Propagate `CancellationToken` if method accepts it

**Before (unverified):**
```csharp
// ❌ Hallucinated API — CaptureMethod doesn't exist on PaymentIntent
var intent = new PaymentIntent
{
    Amount = 1000,
    Currency = "usd",
    CaptureMethod = "manual" // WRONG
};
```

**After (verified against Stripe docs):**
```csharp
// ✅ Verified — CreateAsync with options object
var options = new PaymentIntentCreateOptions
{
    Amount = 1000,
    Currency = "usd",
    CaptureMethod = "manual" // Correct property on options
};
var intent = await paymentIntentService.CreateAsync(options, cancellationToken: ct);
```

### 7. Test Against Real API (If Possible)
- ✅ **Checkpoint:** Code tested in test mode or sandbox environment
- Use test mode APIs (Stripe test keys, Azure dev subscriptions)
- Verify expected responses, error handling, edge cases
- Confirm behavior matches documentation

**Stripe test mode:**
```csharp
// appsettings.Development.json
{
  "Stripe": {
    "SecretKey": "sk_test_...",  // Test mode key
    "WebhookSecret": "whsec_test_..."
  }
}
```

Run integration test:
```powershell
dotnet test --filter "Category=Integration"
```

✅ **Verified** — Stripe returns PaymentIntent in test mode as documented.

## Common Rationalizations

| Rationalization | Reality |
|---|---|
| "I've used this API before" | APIs change between versions. Stripe SDK v47 is different from v42. Always verify. |
| "The AI knows this API" | Models have training cutoff dates. GPT-4 doesn't know .NET 10 or EF Core 9. Verify everything. |
| "I'll look it up if it fails" | Prevention is cheaper than debugging. 2 minutes reading docs saves 30 minutes fixing broken code. |
| "This is a common pattern, it must be right" | Common patterns become obsolete. `DbSet.Add()` vs `DbSet.AddAsync()` — both exist, one is better for async. |
| "Stack Overflow says to do this" | SO answers are often outdated or context-specific. Verify against current official docs. |
| "The compiler will catch mistakes" | Compiler catches syntax, not semantics. Wrong parameter order compiles but breaks at runtime. |
| "I'll test it and see if it works" | Testing without reading docs wastes time. Read first, implement once correctly. |

## Anti-Patterns

| Pattern | Problem | Fix |
|---|---|---|
| **API Guessing** | `stripeClient.Charges.Capture(paymentIntent.Id)` ← method doesn't exist | Read Stripe docs: `PaymentIntents.CaptureAsync()`, not `Charges.Capture()` |
| **Version Assumptions** | Using .NET 8 API examples for .NET 10 project | Check release notes for breaking changes, verify API exists in target version |
| **Tutorial Cargo Culting** | Copy-paste code from 2019 blog post without verification | Verify each API call against current official docs |
| **Incomplete Error Handling** | Catch `Exception` instead of specific Stripe exceptions | Read docs for exceptions thrown: `StripeException`, `RateLimitException`, etc. |
| **Parameter Order Errors** | `CreateAsync(currency, amount)` instead of `(amount, currency)` | Use named parameters or verify signature in IntelliSense |
| **Nullability Ignorance** | Not checking return type nullability (`PaymentIntent?` vs `PaymentIntent`) | Enable nullable reference types, read XML docs for nullability |

## Red Flags

Abort and verify documentation if you observe:

- Compiler error: "Method does not exist" or "No overload matches"
- Runtime exception: `MethodNotFoundException`, `MissingMemberException`
- Unexpected API behavior (returns null when docs say non-null)
- IntelliSense shows different parameters than you expected
- Code compiles but fails integration tests
- API call worked in dev, fails in prod (version mismatch?)
- Stripe/Azure SDK throws "Invalid request" errors
- EF Core query generates SQL that doesn't match intention

## Verification

Before committing code that calls external APIs:

- [ ] Official documentation found and read (not blog post)
- [ ] Method signature verified (name, parameters, return type, nullability)
- [ ] Package version confirmed (`dotnet list package`)
- [ ] Breaking changes reviewed if upgrading versions
- [ ] Existing project usage pattern found and matched
- [ ] Named parameters used for clarity (optional but recommended)
- [ ] CancellationToken propagated if method accepts it
- [ ] Return type nullability handled (`?` checked)
- [ ] Expected exceptions documented in try-catch or XML comments
- [ ] Integration test written against test mode API (Stripe, Azure)
- [ ] Test mode/sandbox environment used for validation

## Domain-Specific Verification (NexTruzt.io)

### Stripe SDK Verification Checklist

Before implementing ANY Stripe API call:

- [ ] Go to https://stripe.com/docs/api and find the endpoint
- [ ] Verify request parameters (amount in cents, currency ISO code)
- [ ] Verify `CaptureMethod` value: `"manual"` | `"automatic"`
- [ ] Check idempotency key requirement (all mutations need it)
- [ ] Verify response object shape (`PaymentIntent`, `Charge`, `Refund`)
- [ ] Check error types: `CardException`, `RateLimitException`, `APIException`
- [ ] Test in Stripe test mode with test keys
- [ ] Verify webhook signature validation if handling webhooks

### EF Core 9 Verification Checklist

Before implementing EF Core queries or migrations:

- [ ] Go to https://learn.microsoft.com/en-us/ef/core/ and find the API
- [ ] Verify LINQ method exists in EF Core 9 (`AsNoTracking()`, `Include()`, etc.)
- [ ] Check if method is async (`ToListAsync()`, `FirstOrDefaultAsync()`)
- [ ] Verify return type nullability (`FirstOrDefault()` → `T?`)
- [ ] Check for PostgreSQL-specific considerations (Npgsql docs)
- [ ] Test query in integration test with Testcontainers
- [ ] Verify generated SQL matches intention (enable sensitive data logging in dev)

### Blazor Lifecycle Verification Checklist

Before overriding Blazor lifecycle methods:

- [ ] Go to https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle
- [ ] Verify method signature: `OnInitializedAsync()` vs `OnParametersSetAsync()`
- [ ] Check if method is sync or async (prefer async)
- [ ] Verify when method is called (first render, parameter changes, etc.)
- [ ] Check if `firstRender` parameter applies (`OnAfterRenderAsync(bool firstRender)`)
- [ ] Confirm disposal pattern (`IDisposable`, `IAsyncDisposable`)
- [ ] Test component lifecycle in integration test

## Integration Points

**Before this skill:**
- You know you need to call an API
- You have a vague idea of what the method is

**After this skill:**
- ✅ API verified against official docs
- ✅ Correct method signature used
- ✅ Return types and nullability handled
- ✅ Error handling matches documented exceptions
- ✅ Integration test confirms API behavior

**Chains well with:**
- `incremental-implementation` — verify APIs BEFORE implementing each slice
- `tdd-coach` — write failing test with verified API signature first
- `debugging-wizard` — use to diagnose API integration failures
- `code-reviewer` — reviewer confirms API usage matches official patterns
