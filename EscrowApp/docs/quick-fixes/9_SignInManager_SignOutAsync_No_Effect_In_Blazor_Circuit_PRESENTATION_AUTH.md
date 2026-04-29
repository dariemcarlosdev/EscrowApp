# QF-009 — `SignInManager.SignOutAsync()` Has No Effect Inside a Blazor Server Circuit

**Date:** 2026-04-16  
**Layer / Concern:** Presentation — Authentication Controls (`Components/Pages/NavBar.razor`, `Program.cs`)  
**Severity:** 🔴 Logout silently fails — auth cookie never cleared, user appears permanently logged in  

---

## Symptom

Clicking the Logout button appeared to do nothing. After navigating back to the home page, the
NavBar still showed the authenticated user's email in the dropdown — as if the logout never happened.

Separately, on a fresh browser session where no login had been performed, the NavBar showed a
previous test user's email and the authenticated dropdown, because the Identity cookie from an
earlier registration/login test was still alive in the browser.

---

## Root Cause

### Why the cookie persisted across sessions

ASP.NET Core Identity issues an HTTP-only authentication cookie on `SignInAsync`. That cookie
persists in the browser until it expires **or** the server explicitly deletes it by sending a
`Set-Cookie` response header with a past expiry date. No logout was ever called, so the cookie
remained valid across restarts.

### Why `SignInManager.SignOutAsync()` silently failed in Blazor

Blazor Server runs over a persistent **SignalR WebSocket** circuit. Inside this circuit there is
**no live HTTP response object** — the connection is a long-lived socket, not a request/response
pair. `SignOutAsync()` works by appending a `Set-Cookie: expires=<past>` header to the HTTP
response, but inside a Blazor circuit that response was sent and completed during the initial
page load. Calling `SignOutAsync()` from an `@onclick` handler writes to a response that has
already been flushed — the header is silently discarded and the browser never receives the
cookie-deletion instruction.

```csharp
// BEFORE — silently does nothing inside a Blazor Server circuit
private async Task HandleLogout()
{
    await SignInManager.SignOutAsync(); // ← no HttpContext.Response to write to
    Nav.NavigateTo("/", forceLoad: true);
}
```

---

## Fix

### 1 — Dedicated minimal API endpoint (`Program.cs`)

A `POST /auth/logout` endpoint runs outside the Blazor circuit — it is a standard HTTP request
with a real `HttpResponse` that can carry the cookie-deletion header.

```csharp
app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();   // ← real HTTP response, cookie deleted correctly
    return Results.LocalRedirect("/");
}).RequireAuthorization().DisableAntiforgery();
```

`DisableAntiforgery()` is used here because the antiforgery token is already embedded in the
form via `<AntiforgeryToken />` in the Blazor markup and the minimal API middleware would
otherwise double-validate.

### 2 — Logout via HTML `<form>` POST, not `@onclick` (`NavBar.razor`)

A standard HTML form `POST` triggers a full HTTP round-trip — the server response carries the
`Set-Cookie` header that deletes the cookie, and the browser follows the redirect to `/`.

```razor
@* POST to /auth/logout — real HTTP POST so the server can write the Set-Cookie deletion header.
   NavigateTo("/auth/logout") inside a Blazor circuit issues a GET — cookie is never cleared. *@
<form method="post" action="/auth/logout">
    <AntiforgeryToken />
    <button type="submit" class="dropdown-item">
        @L["Logout"]
    </button>
</form>
```

### 3 — Removed `SignInManager` injection from `NavBar.razor.cs`

The code-behind no longer needs `SignInManager` or the `HandleLogout` method. The form handles
the full logout flow without any C# event handler.

---

## Rule Going Forward

> **Never call `SignInManager.SignOutAsync()`, `SignInManager.SignInAsync()`, or any method
> that writes authentication cookies from inside a Blazor Server component or event handler.**
>
> Authentication cookie operations require a live HTTP response. Use a dedicated minimal API
> endpoint (or Razor Page) reachable via a real HTTP form POST.

| Context | Can write auth cookies? |
|---|---|
| Minimal API endpoint (`MapPost`) | ✅ Yes — real HTTP response |
| Razor Page `OnPost` handler | ✅ Yes — real HTTP response |
| Blazor `@onclick` / `OnInitializedAsync` | ❌ No — SignalR circuit, no response |
| Blazor `NavigateTo(..., forceLoad: true)` issuing a GET | ❌ No — GET cannot carry form body / antiforgery |

---

## Verification

✅ Clicking **Logout** sends a POST to `/auth/logout`, cookie is deleted, browser redirects to `/`.  
✅ NavBar shows **Log In** link after logout.  
✅ Navigating directly to `/dashboard/client` after logout redirects to `/auth/login`.  
✅ Fresh browser session with no active cookie shows unauthenticated NavBar.
