# QF-012 — `/api/escrow/*` Returns 400 + Redirect to `/auth/login` for API Calls

**Date:** 2026-05-01
**Layer / Concern:** Infrastructure — Authentication (`Program.cs` policy + scheme registration)
**Severity:** 🔴 All REST endpoints under `[Authorize(Policy = "ApiAccess")]` unreachable from non-browser clients

---

## Symptom

`POST https://localhost:7037/api/escrow/hold` with a valid JSON body returned:

```
HTTP/1.1 400
location: https://localhost:7037/auth/login?ReturnUrl=%2Fapi%2Fescrow%2Fhold
content-type: text/html; charset=utf-8
```

Even with `X-Api-Key` header set, the request was challenged by the **cookie** scheme and redirected to the Identity login page.

---

## Root Cause

`Program.cs:68` registers ASP.NET Identity:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(...)
```

`AddIdentity<>` sets the **cookie authentication scheme as the default** for `DefaultAuthenticateScheme` and `DefaultChallengeScheme`. The subsequent block:

```csharp
builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(...)
```

does **not** override the defaults — it only registers the scheme as available. When the `ApiAccess` policy runs without an explicitly pinned scheme, it falls back to the cookie scheme, which challenges by issuing a 302 to `/auth/login`. The browser/Swagger client surfaces that as a 400 with the `location` header.

---

## Fix

Pin the `ApiAccess` policy explicitly to the API key scheme so it never delegates to cookie auth:

```csharp
// Program.cs:143-151
options.AddPolicy("ApiAccess", policy =>
{
    policy.AuthenticationSchemes.Clear();
    policy.AuthenticationSchemes.Add(ApiKeyAuthenticationDefaults.AuthenticationScheme);
    policy.RequireAuthenticatedUser();
});
```

The `AuthenticationSchemes.Add(...)` call forces `[Authorize(Policy = "ApiAccess")]` to invoke the API key handler regardless of the global default.

---

## Verification

```powershell
curl -X POST https://localhost:7037/api/escrow/hold `
  -H "X-Api-Key: <dev-key>" `
  -H "Content-Type: application/json" `
  -d '{...}'
```

Expected: handler executes (200/400/422 from validators) — **no `location` header to `/auth/login`**.

---

## See also

- [QF-013 Blazor Antiforgery rejects API controllers](13_Blazor_Antiforgery_Rejects_API_Controllers_INFRASTRUCTURE_SECURITY.md)
- [QF-014 ApiKey config empty — use user-secrets](14_ApiKey_Config_Empty_UseUserSecrets_INFRASTRUCTURE_AUTH.md)
