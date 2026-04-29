# QF-002 — Circular Dependency in `AuthenticationStateProvider` DI Registration

**Date:** 2026-04-16  
**Layer / Concern:** Infrastructure — Authentication DI (`Program.cs`)  
**Severity:** 🔴 App crash at startup  

---

## Symptom

```
System.InvalidOperationException: A circular dependency was detected for the service
of type 'Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider'.
```

DI container failed to construct the authentication graph on every request.

---

## Root Cause

The original registration mapped `AuthenticationStateProvider` directly to
`RevalidatingIdentityAuthenticationStateProvider`:

```csharp
// Circular: RevalidatingIdentityAuthenticationStateProvider required
// AuthenticationStateProvider in its constructor, which DI resolved back
// to RevalidatingIdentityAuthenticationStateProvider → infinite loop.
builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider>();
```

The class constructor `(AuthenticationStateProvider baseProvider)` asked for the
very interface it was registered as — a textbook circular dependency.

---

## Fix

The class was rewritten to extend `RevalidatingServerAuthenticationStateProvider`
(the framework-provided abstract base) which owns its own internal state machine
and requires no `AuthenticationStateProvider` parameter:

**File:** `EscrowApp/Infrastructure/Auth/RevalidatingIdentityAuthenticationStateProvider.cs`

```csharp
// After — no circular constructor dependency
public sealed class RevalidatingIdentityAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{ ... }
```

**File:** `EscrowApp/Program.cs`

```csharp
// Clean scoped registration — no factory, no ActivatorUtilities
builder.Services.AddScoped<AuthenticationStateProvider,
    RevalidatingIdentityAuthenticationStateProvider>();
```

---

## Verification

✅ App starts without circular dependency exception.  
✅ `CascadingAuthenticationState` resolves `AuthenticationStateProvider` correctly.
