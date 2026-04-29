# MVP-First Development Rules

> Working software > Perfect architecture. Ship a usable product, then iterate.

## MVP Decision Filter

| Question | If YES | If NO |
|----------|--------|-------|
| Does the user see/interact with this? | Build it | Defer |
| Does the app crash without this? | Build it | Defer |
| Is this a security requirement? | Build it | Defer |
| Is this "nice to have" for v1? | Defer | — |
| Building for 10K users when we have 10? | Stop | — |

## Rule of Three for Abstraction

- **1st time:** Write it inline. Ship it.
- **2nd time:** Note the duplication. Ship it.
- **3rd time:** Now extract a shared abstraction with 3 real examples to design from.

## MUST DO in MVP

- ✅ Clean Architecture layers — separation of concerns is free
- ✅ Interfaces for external services — `IEscrowPaymentStrategy` for swappability
- ✅ Input validation — FluentValidation on every command
- ✅ Authentication & authorization — `[Authorize]` everywhere, default deny
- ✅ One happy-path test per feature
- ✅ Code-behind pattern — `.razor` + `.razor.cs` from day one
- ✅ Parameterized queries — never concatenate SQL
- ✅ Structured logging — `ILogger<T>` with structured parameters
- ✅ Dependency injection — no `new SomeService()` in business logic

## MUST NOT in MVP

- ❌ Generic repositories — specific repos per aggregate until 5+ entities
- ❌ CQRS read models — same EF model for reads/writes until perf proves otherwise
- ❌ Event sourcing — simple DB updates; event sourcing is v2+
- ❌ Microservices — modular monolith first; extract on proven bottleneck
- ❌ Message queues — direct calls + MediatR notifications for in-process events
- ❌ Custom middleware — use built-in ASP.NET middleware
- ❌ Abstract factories — inject directly until 3+ runtime implementations
- ❌ Specification pattern — use LINQ Where until 5+ reusable query filters

## Done Definition for MVP Features

1. ✅ Happy path works end-to-end (UI → API → DB → response)
2. ✅ Input validation prevents bad data
3. ✅ Authentication required
4. ✅ Basic error handling (friendly message, not stack trace)
5. ✅ One integration test covers happy path
6. ✅ No hardcoded secrets
7. ✅ Zero build warnings

## When to Break These Rules

- **Compliance requirements** — regulations mandate it
- **Data integrity** — getting it wrong means data loss
- **Security** — never cut corners on auth, validation, secrets
- **Irreversible decisions** — DB schema choices deserve more thought
