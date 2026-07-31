# QF-013 — Blazor `UseAntiforgery()` Rejects REST API POSTs with "Incorrect Content-type"

**Date:** 2026-05-01
**Layer / Concern:** Infrastructure — Security middleware (`app.UseAntiforgery()`), API controllers
**Severity:** 🔴 Every JSON `POST` to `/api/*` returns 400 even with valid auth + valid body

---

## Symptom

After fixing the API key auth scheme pin (QF-012), `POST /api/escrow/hold` still failed:

```
HTTP/1.1 400
content-type: text/html; charset=utf-8

The request has an incorrect Content-type.
```

Reproducible with `Content-Type: application/json` **and** `text/json`. Reproducible with and without `X-Api-Key`. Reproducible from Swagger UI and `curl`.

---

## Root Cause

The host is a Blazor Server app with API controllers mounted on the same pipeline. `Program.cs:331` registers:

```csharp
app.UseAntiforgery();
```

This middleware is **global** — it intercepts every state-changing request (POST/PUT/PATCH/DELETE) and demands a valid antiforgery token, which is normally injected into Blazor `EditForm` posts via `__RequestVerificationToken`. REST clients don't send that token, so the middleware short-circuits with the misleading message **"The request has an incorrect Content-type."** before the controller's auth/model-binding pipeline ever runs.

The error message is misleading: it has nothing to do with `Content-Type`. The middleware classifies any request without a valid token as malformed.

---

## Fix

Apply `[IgnoreAntiforgeryToken]` at the API controller level so all action methods opt out of the global check. API endpoints are protected by the API key + `ApiAccess` policy instead.

```csharp
// EscrowApp/Features/Escrow/Api/EscrowController.cs
[ApiController]
[Route("api/escrow")]
[Authorize(Policy = "ApiAccess")]
[IgnoreAntiforgeryToken]            // ← required when hosted alongside Blazor
public sealed class EscrowController : ControllerBase
{
    ...
}
```

> ⚠️ Apply this attribute to **every** REST controller in the project. Failure to do so resurfaces the same 400 in any new endpoint.

---

## Verification

```powershell
curl -X POST https://localhost:7037/api/escrow/hold `
  -H "X-Api-Key: <dev-key>" `
  -H "Content-Type: application/json" `
  -H "X-Idempotency-Key: ntzt_dev_k1_a3b9f7e2d1c4" `
  -d '{ "clientEmail": "...", "consultantEmail": "...", "amount": 50, ... }'
```

Expected: request reaches `CreateAndHoldFundsHandler`. Response is no longer "incorrect Content-type".

---

## Why not disable antiforgery globally?

Blazor SSR forms (`EditForm` with `[SupplyParameterFromForm]`) need it. Disabling globally would break login, registration, and any future server-rendered form. The per-controller opt-out is the surgical fix.

---

## See also

- [QF-005 EditForm missing FormName](5_EditForm_Missing_FormName_BlazorSSR_PRESENTATION_AUTH.md) — the antiforgery-positive flow this middleware was added for
- [QF-012 ApiAccess policy missing scheme pin](12_ApiAccess_Policy_Missing_Scheme_Pin_INFRASTRUCTURE_AUTH.md)
