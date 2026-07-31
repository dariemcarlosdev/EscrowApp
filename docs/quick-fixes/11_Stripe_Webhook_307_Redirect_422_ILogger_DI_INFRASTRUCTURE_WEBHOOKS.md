# QF-011 — Stripe Webhook Local Delivery: 307 Redirect & 422 ILogger DI Failure

**Date:** 2026-04-30
**Layer / Concern:** Infrastructure — Webhooks (`StripeWebhookEndpoint`), Middleware (`ApiExceptionMiddleware`, `UseHttpsRedirection`), Dev Tooling (Stripe CLI)
**Severity:** 🔴 Webhook deliveries fully broken in local dev — no events processed, no DB updates, no `payment_intent.succeeded` dispatched

---

## Symptom

Two failure modes observed in the same `stripe listen` session, in sequence.

### 1. HTTP forwarding → `[307]` for every event

```
stripe listen --forward-to http://localhost:5093/api/webhooks/stripe

2026-04-30 19:58:48   --> charge.succeeded [evt_3TS4TjIvO8SV0MCn0okc2fex]
2026-04-30 19:58:48  <--  [307] POST http://localhost:5093/api/webhooks/stripe
2026-04-30 19:58:48   --> payment_intent.succeeded [evt_3TS4TjIvO8SV0MCn0Cwio1NT]
2026-04-30 19:58:48  <--  [307] POST http://localhost:5093/api/webhooks/stripe
...
```

App console: **no log entries**. Endpoint never invoked.

### 2. HTTPS forwarding → `[422]` for every event

```
stripe listen --forward-to https://localhost:7037/api/webhooks/stripe

2026-04-30 20:19:36   --> charge.succeeded [evt_3TS4nrIvO8SV0MCn20UIBKNL]
2026-04-30 20:19:37  <--  [422] POST https://localhost:7037/api/webhooks/stripe
...
```

App console: warning log `Business rule violation on /api/webhooks/stripe` from `ApiExceptionMiddleware` — but no `✅ Webhook verified and parsed` line.

---

## Root Cause

### Issue 1 — `307` (HTTP forwarding)

`Program.cs:322` registers `app.UseHttpsRedirection()`, which 307-redirects every HTTP request to `https://localhost:7037`. **Stripe CLI does not follow redirects** — it records the response code and moves to the next event. The webhook handler never executes; signature verification, dispatch, and DB updates are all skipped.

The HTTP port (`5093`) is only valid for the Development-only `GET /api/webhooks/stripe` diagnostic endpoint when accessed directly in a browser (which auto-follows redirects).

### Issue 2 — `422` (HTTPS forwarding)

`StripeWebhookEndpoint.HandleAsync` declared this parameter:

```csharp
[FromServices] ILogger logger,   // ← non-generic
```

ASP.NET Core's DI container **does not register the non-generic `ILogger`**. It registers:

- `ILogger<T>` (open generic, resolved per category)
- `ILoggerFactory` (singleton)

Resolving plain `ILogger` during minimal-API parameter binding throws `InvalidOperationException: Unable to resolve service for type 'Microsoft.Extensions.Logging.ILogger' while attempting to activate ...`.

Because the failure happens **before** the method body executes, the handler's own `try/catch` never runs. The exception bubbles up to `ApiExceptionMiddleware.InvokeAsync`, whose `catch (InvalidOperationException)` branch maps it to **422 Unprocessable Entity** with title `"Business Rule Violation"`:

```csharp
// ApiExceptionMiddleware.cs:31-36
catch (InvalidOperationException ex)
{
    logger.LogWarning(ex, "Business rule violation on {Path}", context.Request.Path);
    await WriteProblemDetails(context, HttpStatusCode.UnprocessableEntity,
        "Business Rule Violation", ex.Message);
}
```

This makes any DI resolution failure on `/api/*` look like a domain validation error — a misleading symptom for what is actually a wiring bug.

---

## Fix

### 1. HTTPS-only forwarding (documentation)

`docs/Test/local-stripe-cli-webhook-test.md` updated in three places:

- **Quick Start** — forwarding command flipped to `https://localhost:7037` with an inline note that HTTP returns 307.
- **Endpoint Reference table** — HTTPS marked recommended, HTTP marked "browser diagnostics only — do not use as `--forward-to` target".
- **Step 1 (Start Stripe CLI forwarding)** — code blocks reordered to HTTPS first; added a `> ⚠️` callout with the redirect explanation; kept `--skip-verify` as a fallback when the dev cert is not trusted.
- **Troubleshooting** — added a top row mapping `[307] for every event` → `UseHttpsRedirection()` not followed by Stripe CLI → switch to HTTPS forwarding.

### 2. ILoggerFactory injection (code)

`EscrowApp/Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs`, in `HandleAsync`:

```diff
 public static async Task<IResult> HandleAsync(
     HttpContext httpContext,
     [FromServices] StripeSignatureVerifier verifier,
     [FromServices] IOptions<StripeWebhookOptions> webhookOptions,
     [FromServices] IPublisher mediator,
-    [FromServices] ILogger logger,
+    [FromServices] ILoggerFactory loggerFactory,
     CancellationToken ct)
 {
+    // Non-generic ILogger is NOT registered in DI by default — only ILogger<T>
+    // and ILoggerFactory. Resolving ILogger directly throws InvalidOperationException,
+    // which ApiExceptionMiddleware would translate to 422. Use the factory to create
+    // a category-named logger instead.
+    var logger = loggerFactory.CreateLogger(nameof(StripeWebhookEndpoint));
     try
     {
```

`ILoggerFactory` is registered by default by `WebApplicationBuilder`, so no `Program.cs` change is required.

---

## Verification

1. Stop the app.
2. Set the active `stripe listen` signing secret:
   ```powershell
   dotnet user-secrets set "Stripe:Webhook:EndpointSecret" "whsec_<from-stripe-listen>" --project EscrowApp
   ```
3. `dotnet run --project EscrowApp`
4. In another terminal: `stripe trigger payment_intent.succeeded`

**Expected (and observed):**
```
2026-04-30 20:24:28  <--  [204] POST https://localhost:7037/api/webhooks/stripe [charge.succeeded]
2026-04-30 20:24:28  <--  [204] POST https://localhost:7037/api/webhooks/stripe [payment_intent.succeeded]
2026-04-30 20:24:28  <--  [204] POST https://localhost:7037/api/webhooks/stripe [payment_intent.created]
2026-04-30 20:24:32  <--  [204] POST https://localhost:7037/api/webhooks/stripe [charge.updated]
```

App logs include `✅ Webhook verified and parsed` for each delivery and `📨 Dispatching PaymentIntentSucceeded to MediatR` for the one event the MVP handler dispatches.

---

## Reusable Lessons

1. **Never declare `[FromServices] ILogger` (non-generic) in minimal-API endpoints.** Use `ILoggerFactory` + `CreateLogger("category")` for static endpoint methods, or `ILogger<T>` for typed categories. Repeat this check across any future endpoint added under `Features/**/*Api*` or `Infrastructure/**/Endpoints/*`.

2. **`ApiExceptionMiddleware`'s `InvalidOperationException → 422` mapping disguises DI failures as business-rule violations** on `/api/*` routes. When debugging mysterious 422s, inspect the app console for `Unable to resolve service for type ...` warnings *before* suspecting FluentValidation or domain logic.

3. **Stripe CLI does not follow HTTP redirects.** Any `--forward-to` target behind `UseHttpsRedirection()` must be HTTPS. The CLI's silent `[307]` failure mode is easy to misread as a connectivity problem when it is actually a redirect-not-followed problem.

---

## Files Modified

| File | Change |
|---|---|
| `EscrowApp/Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs` | Replaced `[FromServices] ILogger logger` with `[FromServices] ILoggerFactory loggerFactory`; instantiated category logger inside the method |
| `docs/Test/local-stripe-cli-webhook-test.md` | Flipped HTTP→HTTPS recommendation in Quick Start, Endpoint Reference, and Step 1; added `[307]` Troubleshooting row |
| `docs/quick-fixes/11_Stripe_Webhook_307_Redirect_422_ILogger_DI_INFRASTRUCTURE_WEBHOOKS.md` | This document |

## Reference Files (read-only context)

- `EscrowApp/Program.cs:322` — `app.UseHttpsRedirection()` registration (cause of 307)
- `EscrowApp/Program.cs:345-350` — webhook route mapping with `.DisableAntiforgery()`
- `EscrowApp/Infrastructure/Middleware/ApiExceptionMiddleware.cs:31-36` — `InvalidOperationException → 422` translation
- `EscrowApp/Infrastructure/Webhooks/Stripe/StripeSignatureVerifier.cs` — `EventUtility.ConstructEvent` invocation
- `EscrowApp/EscrowApp.csproj:25` — `Stripe.net 51.0.0` package reference

## Related Insight

`NexSynapse/docs/insights/insight-log.md` entry #102 — same debugging session captured for cross-session recall via `search_insights "stripe webhook"`.
