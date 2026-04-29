# Scope Control Patterns — Deep Dive

> Load this reference when blocking over-engineering or scope creep in implementation.

## The Scope Creep Kill Chain

Scope creep follows a predictable pattern. Intercept at each stage:

```
1. "While I'm here, I should also..."     → STOP. File a separate task.
2. "This would be cleaner if I..."         → STOP. Does the user see the difference?
3. "We'll need this eventually..."         → STOP. Eventually is not today.
4. "It's only 30 more minutes..."          → STOP. 30 minutes × 10 features = 5 hours lost.
5. "Best practice says we should..."       → STOP. Best practice for 10M users ≠ best practice for 10.
```

## Pattern: Minimum Viable Implementation

For every feature, apply this reduction cascade:

### Level 1 — Can we hardcode it?

| Instead of... | Hardcode as... | Upgrade trigger |
|---|---|---|
| Configuration service | `appsettings.json` values | 3+ environments need different values |
| Admin panel | Direct database queries | 100+ admin operations per week |
| Feature flags | `#if DEBUG` or bool constant | 3+ features need runtime toggling |
| Multi-tenant routing | Single-tenant deployment | Second paying customer requests it |
| Dynamic pricing | Constant `0.015m` (1.5%) | Pricing strategy validated with users |

### Level 2 — Can we use a built-in?

| Instead of... | Use built-in... | Upgrade trigger |
|---|---|---|
| Custom auth middleware | `[Authorize(Policy = "...")]` | Never — built-in is correct |
| Custom exception handler | `UseExceptionHandler()` | Need per-endpoint error shapes |
| Custom validation framework | FluentValidation (already in project) | Never |
| Custom logging pipeline | `ILogger<T>` + Serilog | Need log aggregation at scale |
| Custom retry logic | Polly (already in project) | Never |

### Level 3 — Can we defer the abstraction?

| Instead of... | Write directly... | Abstract when... |
|---|---|---|
| `IRepository<T>` | `IEscrowTransactionRepository` | 5+ repositories share identical patterns |
| `INotificationService` | Direct email send in handler | 3+ handlers need notifications |
| Generic error result type | `throw` + `catch` in middleware | Error handling becomes a pattern across 5+ handlers |
| Event-driven architecture | Direct method calls | 3+ consumers need the same event |
| Base component class | Repeated code in 2 components | Same pattern in 3+ components |

## Pattern: Scope Boundary Enforcement

### File Count Rule

A well-scoped MVP feature touches **at most 8 files**:

```
1. Command.cs           (MediatR command record)
2. CommandValidator.cs   (FluentValidation)
3. Handler.cs           (MediatR handler)
4. Result.cs            (Response DTO)
5. Page.razor           (Blazor markup)
6. Page.razor.cs        (Code-behind)
7. Page.razor.css       (Scoped styles)
8. Repository method    (Add method to existing interface + implementation)
```

**More than 8 files? The feature is too big.** Split it.

### Time Box Rule

| Task Type | Max Time | If Over Budget |
|---|---|---|
| New MediatR handler | 2 hours | Simplify scope — remove edge case handling |
| New Blazor page | 2 hours | Bootstrap defaults only — no custom CSS |
| New repository method | 30 min | Simplest LINQ query that works |
| FluentValidation rules | 30 min | Validate only required fields and obvious bounds |
| Integration test | 1 hour | Happy path only — one test, one assertion |
| Database migration | 30 min | Add column/table only — no data migration |
| Bug fix | 1 hour | Fix the symptom, file a tech debt ticket for root cause if complex |

**Over time-box? Ship what you have, defer the rest.**

## Pattern: The "10 Users" Test

Before building anything, answer:

> "If I have exactly 10 paying users, do they need this?"

| Feature | 10 Users Need It? | Verdict |
|---|---|---|
| Create escrow transaction | Yes — core product | BUILD |
| Transaction list view | Yes — they need to see their money | BUILD |
| Pagination on transaction list | No — 10 users won't have 100+ transactions | DEFER |
| Full-text search across transactions | No — manual scanning works for <50 items | DEFER |
| Real-time notifications via SignalR | No — email + page refresh works | DEFER |
| Export transactions to CSV | No — screenshot or manual copy works | DEFER |
| Mobile-responsive UI | Maybe — test with 10 users first | DEFER (Bootstrap responsive is free) |
| Multi-language (es-MX) | Maybe — depends on first 10 users | BUILD (already scaffolded, low cost) |
| Dark mode | No — cosmetic, zero revenue impact | DEFER |
| API documentation (Swagger) | No — no API consumers yet | DEFER for public, keep dev-only |

## Pattern: Dependency Justification

Adding a new NuGet package or npm dependency requires justification:

```
PACKAGE: [Name]
ALREADY IN PROJECT: [Yes/No]
WHAT IT REPLACES: [Manual code I would otherwise write]
LINES OF CODE SAVED: [Estimate]
SECURITY RISK: [Is this actively maintained? Last update?]
VERDICT: [APPROVED / REJECTED]
```

**Rules:**
- If the package is already in the project → use it freely
- If a built-in ASP.NET feature does the same thing → use built-in, reject package
- If the package saves <50 lines of code → write it manually
- If the package hasn't been updated in 12+ months → reject

## Anti-Pattern Gallery

### Anti-Pattern: The Premature Microservice

```
❌ "Let's split payments into a separate service with its own database and API"

✅ MVP: Monolith with Clean Architecture boundaries.
   Split to microservice WHEN: payment processing latency affects other features
   AND you have >1000 transactions/day.
```

### Anti-Pattern: The Generic Repository

```
❌ public interface IRepository<T> where T : BaseEntity
   {
       Task<T> GetByIdAsync(Guid id);
       Task<IReadOnlyList<T>> GetAllAsync();
       Task AddAsync(T entity);
       ...
   }

✅ MVP: Specific repositories with only the methods the domain needs.
   public interface IEscrowTransactionRepository
   {
       Task<EscrowTransaction?> GetByIdAsync(int id, CancellationToken ct);
       Task AddAsync(EscrowTransaction transaction, CancellationToken ct);
       Task UpdateAsync(EscrowTransaction transaction, CancellationToken ct);
   }
```

### Anti-Pattern: The Event Sourcing Fantasy

```
❌ "Every state change should be an event, stored immutably, replayed to rebuild state"

✅ MVP: Update the row in PostgreSQL. Publish a domain event for side effects.
   Event sourcing WHEN: audit requirements demand full replay capability
   AND you have regulatory compliance needs.
```

### Anti-Pattern: The Configuration Astronaut

```
❌ "Let's make the platform fee configurable per tenant with A/B testing support"

✅ MVP: const decimal PlatformFeeRate = 0.015m;
   Make it configurable WHEN: you need different rates for different customer segments
   AND you have data proving the rates should differ.
```
