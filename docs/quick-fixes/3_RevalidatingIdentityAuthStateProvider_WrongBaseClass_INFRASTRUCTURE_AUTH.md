# QF-003 — `RevalidatingIdentityAuthenticationStateProvider` Wrong Base Class

**Date:** 2026-04-16  
**Layer / Concern:** Infrastructure — Authentication (`Infrastructure/Auth/`)  
**Severity:** 🔴 Circuit crash on every page render  

---

## Symptom

```
System.InvalidOperationException: Do not call GetAuthenticationStateAsync outside
of the DI scope for a Razor component. Typically, this means you can call it only
within a Razor component or inside another DI service that is resolved for a
Razor component.
   at Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider
      .GetAuthenticationStateAsync()
```

Every circuit terminated immediately after establishing the SignalR connection.

---

## Root Cause

The custom class extended `AuthenticationStateProvider` and manually delegated
to `ServerAuthenticationStateProvider` by calling its `GetAuthenticationStateAsync()`:

```csharp
// WRONG — ServerAuthenticationStateProvider enforces strict DI scope rules.
// It throws when called from outside a Blazor component's rendering pipeline.
public sealed class RevalidatingIdentityAuthenticationStateProvider(
    AuthenticationStateProvider baseProvider)          // ← took ServerAuthenticationStateProvider
    : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var state = await _baseProvider.GetAuthenticationStateAsync(); // ← throws
        ...
    }
}
```

`ServerAuthenticationStateProvider` validates that it is called from within a
component's rendering context and throws `InvalidOperationException` when invoked
from any other location (timer callbacks, factory methods, etc.).

---

## Fix

Rewrote to extend `RevalidatingServerAuthenticationStateProvider` — the framework
abstract base that owns the rendering context lifecycle, manages a background
revalidation timer, and creates properly-scoped DI contexts per tick. Validation
is done via Identity's security stamp instead of re-calling `GetAuthenticationStateAsync`.

**File:** `EscrowApp/Infrastructure/Auth/RevalidatingIdentityAuthenticationStateProvider.cs`

```csharp
public sealed class RevalidatingIdentityAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    private static async Task<bool> ValidateSecurityStampAsync(
        UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return false;
        if (!userManager.SupportsUserSecurityStamp) return true;

        var principalStamp = principal.FindFirstValue(
            new ClaimsIdentityOptions().SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);
        return principalStamp == userStamp;
    }
}
```

---

## Verification

✅ Circuit connects without `InvalidOperationException`.  
✅ Security stamp validation works on 30-minute revalidation cycle.  
✅ Password change / role change forces re-authentication on next revalidation tick.
