# QF-010 — CSP Blocks CDN Fonts/Styles & BrowserLink Cross-Scheme Cookie Warnings

**Date:** 2026-04-16  
**Layer / Concern:** Infrastructure — Security Headers (`Program.cs`), Dev Tooling (`launchSettings.json`)  
**Severity:** 🟡 UI broken in dev (missing fonts/icons), console noise from BrowserLink — no production impact  

---

## Symptom

Browser DevTools showed multiple errors and warnings:

1. Google Fonts stylesheet blocked — `style-src 'self' 'unsafe-inline'` did not allow `https://fonts.googleapis.com`
2. Bootstrap Icons CSS blocked — same `style-src` issue for `https://cdn.jsdelivr.net`
3. Bootstrap Icons `.woff2` font file blocked — `font-src 'self'` did not allow `https://cdn.jsdelivr.net`
4. BrowserLink SignalR negotiate requests blocked — `connect-src` did not allow `http://localhost:*`
5. Hot-reload / Blazor WebSocket blocked — `connect-src` did not allow `ws://localhost:*`
6. Chrome warning: "A cookie was not sent to an insecure origin from a secure context" for 3 cookies:
   - `.AspNetCore.Culture`
   - `.AspNetCore.Antiforgery.mCr9lId3KRs`
   - `.AspNetCore.Identity.Application`

---

## Root Cause

### CSP Too Restrictive (Items 1–5)

The original CSP in `Program.cs` was:

```
default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;
```

No `font-src` directive at all, no CDN origins in `style-src`, no `connect-src` for dev tooling.

### Cross-Scheme Cookie Warning (Item 6)

Visual Studio injects BrowserLink via its debugger pipeline (an `IHostingStartup` assembly). BrowserLink
makes `http://localhost:{random-port}` XHR requests from the HTTPS page. Chrome's cross-scheme SameSite
enforcement detects the HTTPS → HTTP scheme mismatch and refuses to send cookies, logging the warning.

This is **protective behavior** — the cookies are never leaked. The warning confirms the browser blocked
cookie transmission to the insecure origin.

---

## Fix

### 1. CSP Updated (`Program.cs`)

```csharp
var connectSrc = app.Environment.IsDevelopment()
    ? "connect-src 'self' ws://localhost:* wss://localhost:* http://localhost:*;"
    : "connect-src 'self' wss:;";

context.Response.Headers.Append(
    "Content-Security-Policy",
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline'; " +
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
    "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
    "img-src 'self' data:; " +
    connectSrc);
```

| Directive | Origin Added | Why |
|---|---|---|
| `style-src` | `https://fonts.googleapis.com` | Google Fonts CSS (`@font-face` rules) |
| `style-src` | `https://cdn.jsdelivr.net` | Bootstrap Icons CSS |
| `font-src` | `https://fonts.gstatic.com` | Google Fonts `.woff2` files |
| `font-src` | `https://cdn.jsdelivr.net` | Bootstrap Icons `.woff2` files |
| `connect-src` (dev) | `ws://localhost:*` `wss://localhost:*` | Blazor SignalR + hot-reload |
| `connect-src` (dev) | `http://localhost:*` | BrowserLink (VS-injected, cannot be disabled via config) |
| `connect-src` (prod) | `wss:` | Blazor SignalR only |

### 2. Cookie Security Hardened (`Program.cs`)

```csharp
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.Cookie.HttpOnly = true;
    opts.LoginPath = "/auth/login";
    opts.AccessDeniedPath = "/auth/access-denied";
});

builder.Services.AddAntiforgery(opts =>
{
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
});
```

Culture cookie also tightened: `SameSite=Lax` → `SameSite=Strict`.

### 3. BrowserLink Suppression Attempted (`launchSettings.json`)

```json
"browserLink": false,
"ASPNETCORE_HOSTINGSTARTUPASSEMBLIES": ""
```

**Result:** VS debugger pipeline bypasses both settings. These are effective for `dotnet run` only.
To fully disable BrowserLink: **Tools → Options → Projects and Solutions → ASP.NET Core → Auto build and refresh → None**.

---

## Remaining Known Issue

The 3-cookie cross-scheme warning persists when running under VS debugger with BrowserLink active.
This is a **cosmetic dev-only artifact** — cookies are protected, not leaked. It will never appear
in production or when running via `dotnet run` / `dotnet watch`.

---

## Files Modified

| File | Change |
|---|---|
| `Program.cs` | CSP directives updated, dev/prod `connect-src` split, cookie hardening added |
| `Properties/launchSettings.json` | `browserLink: false`, `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES: ""` |

---

## Verification

- [x] Google Fonts loads without CSP violation
- [x] Bootstrap Icons CSS and `.woff2` fonts load without CSP violation
- [x] Blazor Server WebSocket connects without CSP violation
- [x] Hot-reload connects without CSP violation
- [x] All cookies: `Secure=Always`, `SameSite=Strict`, `HttpOnly=true`
- [x] Production CSP contains no `localhost` origins
- [x] Cross-scheme cookie warning is cosmetic only — no leakage

---

## Prevention

- New external CDN resources in `App.razor` → add origin to the matching CSP directive in `Program.cs`
- Never add `'unsafe-eval'` to `script-src` without security review
- Test CSP in both Development and Production configurations
