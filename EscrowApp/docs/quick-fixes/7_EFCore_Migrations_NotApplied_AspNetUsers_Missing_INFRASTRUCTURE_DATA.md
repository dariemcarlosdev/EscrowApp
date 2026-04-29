# QF-007 — EF Core Migrations Not Applied — `AspNetUsers` Table Missing

**Date:** 2026-04-16  
**Layer / Concern:** Infrastructure — Data / Database (`Data/Migrations/`)  
**Severity:** 🔴 PostgreSQL error on first user action  

---

## Symptom

```
Registration failed: 42P01: relation "AspNetUsers" does not exist POSITION: 307
```

PostgreSQL error `42P01` = "undefined table". Any operation touching Identity
tables (register, login, logout) failed with this error.

---

## Root Cause

Five EF Core migrations existed in `EscrowApp/Migrations/` but had never been
applied to the PostgreSQL database. The critical migration was:

```
20260416011350_AddIdentityToEscrowDb
```

This migration creates all ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`,
`AspNetRoleClaims`) plus the `IX_AspNetUsers_ActorId` index linking users to
domain `Actor` entities.

Migrations had been generated (code exists) but `dotnet ef database update` had
not been run against the target PostgreSQL instance.

---

## Fix

Stopped the debugger (required — EF CLI cannot build while the binary is locked
by a running process), then applied all pending migrations:

```powershell
cd EscrowApp
dotnet ef database update
```

Output confirmed all 5 migrations applied:

| Migration | Applied |
|---|---|
| `20260404012529_InitialCreate` | ✅ |
| `20260404193912_HybridIdentityAndAgnosticPersistence` | ✅ |
| `20260404194829_DisputeFundsSlice` | ✅ |
| `20260415003556_AddPlatformFeeToEscrowTransaction` | ✅ |
| `20260416011350_AddIdentityToEscrowDb` | ✅ |

---

## Prevention

Add to `Program.cs` startup (acceptable for MVP / single-instance deployments):

```csharp
// Apply pending migrations automatically on startup (MVP only)
using var scope = app.Services.CreateScope();
await scope.ServiceProvider
    .GetRequiredService<EscrowDbContext>()
    .Database.MigrateAsync();
```

For production multi-instance deployments, run migrations as a pre-deploy step in CI/CD.

---

## Verification

✅ User registration succeeds — row inserted into `AspNetUsers`.  
✅ `dotnet ef database update` reports `No migrations were applied. Already up to date.`
