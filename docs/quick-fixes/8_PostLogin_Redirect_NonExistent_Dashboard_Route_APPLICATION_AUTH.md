# QF-008 — Post-Login Redirect to Non-Existent `/dashboard` Route

**Date:** 2026-04-16  
**Layer / Concern:** Application — Auth Feature / Presentation (`Features/Auth/Login/`, `Components/Pages/Auth/`)  
**Severity:** 🔴 "Not Found" page shown immediately after successful login  

---

## Symptom

After a successful login, the browser navigated to `/dashboard` and showed:

```
Not Found
Sorry, the content you are looking for does not exist.
```

The login itself succeeded (Identity cookie set correctly) but the user could
not reach any dashboard.

---

## Root Cause

`Login.razor.cs` had a hardcoded redirect target:

```csharp
Navigation.NavigateTo("/dashboard", replace: true);
```

The application has **no `/dashboard` route**. The actual routes are:

| Route | Component |
|---|---|
| `/dashboard/client` | `ClientDashboard.razor` |
| `/dashboard/consultant` | `ConsultantDashboard.razor` |

Additionally, the correct destination depends on the user's role — a Consultant
must land on `/dashboard/consultant`, a Client (or user with no assigned role)
on `/dashboard/client`.

---

## Fix

**`LoginResult` extended with `RedirectUrl`:**

```csharp
// Features/Auth/Login/LoginCommand.cs
public sealed record LoginResult(bool Success, string? ErrorMessage = null, string RedirectUrl = "/")
{
    public static LoginResult SuccessResult(string redirectUrl) => new(true, null, redirectUrl);
    public static LoginResult FailureResult(string message) => new(false, message);
}
```

**`LoginCommandHandler` resolves role and returns correct URL:**

```csharp
// Features/Auth/Login/LoginCommandHandler.cs
if (result.Succeeded)
{
    var user = await userManager.FindByEmailAsync(request.Email);
    var roles = user is not null ? await userManager.GetRolesAsync(user) : [];

    var redirectUrl = roles.Contains("Consultant")
        ? "/dashboard/consultant"
        : "/dashboard/client";

    return LoginResult.SuccessResult(redirectUrl);
}
```

**`Login.razor.cs` uses the result URL instead of a hardcoded path:**

```csharp
if (result.Success)
{
    Navigation.NavigateTo(result.RedirectUrl, replace: true);
}
```

---

## Related Task

> ⚠️ **Open item:** When a user registers for the first time, no role is assigned.
> Until role assignment is implemented, all new users redirect to `/dashboard/client`
> regardless of intent. See task checklist — "Assign role on first registration".

---

## Verification

✅ Successful login navigates to `/dashboard/client` (default — no role assigned yet).  
✅ `LoginResult.RedirectUrl` propagates correctly from handler → component.  
✅ `LoginCommandHandlerTests` updated to pass `UserManager` mock and assert `RedirectUrl`.
