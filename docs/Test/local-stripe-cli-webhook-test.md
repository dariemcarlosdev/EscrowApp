# Local Stripe Webhook Testing Guide

> Step-by-step manual test for the EscrowApp Stripe webhook endpoint using the Stripe CLI.
> Validates signature verification, event dispatch, and correlation against a real `EscrowTransaction`.

## Glossary

Quick definitions for the Stripe-specific identifiers and acronyms used throughout this guide.

| Term | Meaning | Where it shows up |
|---|---|---|
| **PI** / **PaymentIntent** | Stripe's core payment object representing a single payment lifecycle (create → authorize → capture → succeed/fail). EscrowApp creates one PI per `EscrowTransaction`. | Logs as `pi_...` (e.g. `pi_3OxYz123...`) |
| **`pi_...`** | The Stripe ID of a PaymentIntent. Stored in `EscrowTransaction.ExternalReference`; this is what links a Stripe PI to its EscrowApp row. | DB column `ExternalReference`, app logs, Dashboard URL |
| **Event** / **`evt_...`** | A webhook event wrapping a state change of some Stripe object (often a PI). One PI can produce many events: `payment_intent.created`, `payment_intent.amount_capturable_updated`, `payment_intent.succeeded`, etc. | Stripe CLI feed, Dashboard → Developers → Events |
| **`whsec_...`** | Webhook signing secret used to verify the `Stripe-Signature` header on incoming POSTs. The CLI prints an **ephemeral** one per `stripe listen` session; Dashboard-registered endpoints have a **permanent** one. | `Stripe:Webhook:EndpointSecret` config key |
| **`pm_...`** | A PaymentMethod ID (e.g. `pm_card_visa`). Used as input to create/confirm a PI. | `paymentMethodId` field in API requests |
| **Manual capture** | Stripe mode where a PI is only **authorized** on creation and remains in `requires_capture` until an explicit capture call. EscrowApp uses this for hold-then-release. `payment_intent.succeeded` fires only after capture. | `Services/Strategies/StripePaymentStrategy.cs` |
| **Test mode** | The non-production Stripe environment. Uses `sk_test_...` keys; charges no real money. **All examples in this guide assume test mode.** | Dashboard toggle (top-left); `sk_test_...` prefix |

## Quick Start

For developers already familiar with Stripe CLI:

```bash
# 1. Forward Stripe events to the local app (copy the printed whsec_... value)
#    Use HTTPS — the app calls UseHttpsRedirection(), so http:// returns 307 and Stripe CLI does not follow redirects.
stripe listen --forward-to https://localhost:7037/api/webhooks/stripe

# 2. Set the printed secret as Stripe:Webhook:EndpointSecret (see Step 3 below)
dotnet user-secrets set "Stripe:Webhook:EndpointSecret" "whsec_..." --project EscrowApp

# 3. Start the app, then in a third terminal:
stripe trigger payment_intent.succeeded     # transport test
stripe events resend <evt_id>               # correlation test against a real PI
```

Expected: `204 No Content` on each delivery, with logs showing `✅ Webhook verified and parsed`.

---

## What This Guide Covers

| Goal | Validates |
|---|---|
| **Transport test** | Stripe CLI can reach the endpoint and signature verification passes |
| **Correlation test** | A real `PaymentIntent` created by the app maps to an `EscrowTransaction` via `ExternalReference` |
| **Response contract** | Endpoint returns `204` on success, `400`/`401` on rejection, `500` on unexpected error |

The MVP webhook handler only dispatches `payment_intent.succeeded`. Other event types are logged and ignored — that is expected.

## Endpoint Reference

EscrowApp's `Properties/launchSettings.json` exposes the webhook on **both** HTTP and HTTPS:

| Profile | URL | When to use |
|---|---|---|
| HTTPS | `https://localhost:7037/api/webhooks/stripe` | **Recommended.** Required because `Program.cs` calls `app.UseHttpsRedirection()`, which 307-redirects HTTP requests. Stripe CLI does **not** follow redirects, so HTTP forwarding silently fails. |
| HTTP | `http://localhost:5093/api/webhooks/stripe` | Browser diagnostics only (`GET`). Do **not** use as `--forward-to` target — the 307 redirect prevents the handler from running. |

In `Development`, `GET /api/webhooks/stripe` returns a small diagnostic payload so the route is visible from a browser. **Stripe still uses `POST` only** — `GET` is a developer convenience.

## Prerequisites

1. Stripe CLI installed and logged in (`stripe login`).
2. A Stripe **test mode** secret key configured for `Stripe:SecretKey`.
3. Local PostgreSQL reachable; EF Core migrations applied.
4. `dotnet` SDK matching the project target (.NET 10).
5. (HTTPS only) `dotnet dev-certs https --trust` has been run successfully.

> ⚠️ **Never use a live-mode secret locally.** All examples assume Stripe **test mode**.

---

## Step-by-Step Walkthrough

### Step 1 — Start Stripe CLI forwarding

In a dedicated terminal (keep it open for the entire test session):

```bash
# HTTPS (recommended) — requires `dotnet dev-certs https --trust` once
stripe listen --forward-to https://localhost:7037/api/webhooks/stripe

# If your dev cert is not trusted, bypass cert validation:
stripe listen --forward-to https://localhost:7037/api/webhooks/stripe --skip-verify
```

> ⚠️ **Do not forward to `http://localhost:5093`.** The app's `UseHttpsRedirection()` middleware returns `307` for HTTP requests, and Stripe CLI does not follow redirects — your handler will never execute and the CLI will show `[307]` for every event. See the Troubleshooting table.

**Verify:**

- The CLI prints: `> Ready! Your webhook signing secret is whsec_...`
- **Copy that `whsec_...` value** — Step 2 needs it.
- The CLI stays running and shows `<-- forward` lines as events arrive.

> The signing secret rotates each `stripe listen` session. If you stop and restart the CLI, you must redo Step 2.

### Step 2 — Inject the webhook signing secret

The app reads `Stripe:Webhook:EndpointSecret` via `IOptions<StripeWebhookOptions>`. Use **one** of the following (preference order):

#### Option A — `dotnet user-secrets` (recommended)

Per-developer, never committed, scoped to the project:

```powershell
cd EscrowApp
dotnet user-secrets set "Stripe:Webhook:EndpointSecret" "whsec_xxx_from_step_1"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_xxx"   # if not already set
```

#### Option B — Environment variable

Useful in CI or when running outside the IDE:

```powershell
# PowerShell — current session only
$env:Stripe__Webhook__EndpointSecret = "whsec_xxx_from_step_1"
dotnet run --project EscrowApp
```

```bash
# bash / zsh
export Stripe__Webhook__EndpointSecret="whsec_xxx_from_step_1"
dotnet run --project EscrowApp
```

> Note the **double underscore** (`__`) — that's how .NET maps env vars to nested config keys.

#### Option C — `launchSettings.json` env block

Convenient but **must not be committed** with a real secret. Edit `EscrowApp/Properties/launchSettings.json` locally and add to the relevant profile:

```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "Stripe__Webhook__EndpointSecret": "whsec_xxx_from_step_1"
}
```

> ⚠️ The placeholder `whsec_test_secret` in `appsettings.json` is **not valid** for signature verification. It exists only to satisfy startup configuration binding. Always override it locally.

### Step 3 — Start the application

```powershell
dotnet run --project EscrowApp
```

**Verify:**

- Startup logs show no Stripe option binding errors.
- `GET http://localhost:5093/api/webhooks/stripe` returns the diagnostic JSON (status `ready`, accepted method `POST`).
- The Stripe CLI from Step 1 is still running.

### Step 4 — Transport test (`stripe trigger`)

Confirms the wire path: CLI → app → signature verification → MediatR dispatch.

In a third terminal:

```bash
stripe trigger payment_intent.succeeded
```

**Verify:**

- Stripe CLI logs: `--> payment_intent.succeeded ...` then `<-- [204] POST http://localhost:5093/api/webhooks/stripe`.
- App logs show:
  - `✅ Webhook verified and parsed: EventId=evt_..., EventType=payment_intent.succeeded`
  - `📨 Dispatching PaymentIntentSucceeded to MediatR: PaymentIntentId=pi_...`

> ✅ **Acceptable outcome:** A downstream handler may log that the `PaymentIntent` is unknown — `stripe trigger` creates a brand-new test PI that has no matching `EscrowTransaction`. The transport path is still proven.

### Step 5 — Create a real `EscrowTransaction` for correlation

Use the public hold endpoint (do **not** insert DB rows manually):

```bash
curl -X POST http://localhost:5093/api/escrow/hold \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <your-local-api-key>" \
  -d '{
    "clientEmail": "client@example.com",
    "consultantEmail": "consultant@example.com",
    "amount": 50.00,
    "serviceDescription": "Webhook correlation test",
    "paymentMethodId": "pm_card_visa"
  }'
```

**Verify:**

- The response contains the new transaction with `externalReference = "pi_..."` and `externalProvider = "Stripe"`.
- Note the `pi_...` (PaymentIntent ID) and the corresponding `evt_...` shown in the Stripe CLI feed (it logs the auth events automatically).
- In the [Stripe Dashboard → Developers → Events (test)](https://dashboard.stripe.com/test/events), find the event id (`evt_...`) for the `payment_intent.succeeded` tied to your `pi_...`. If the PI is only authorized (not captured), trigger capture via the release flow first.

#### What's saved in the `Transactions` table

`POST /api/escrow/hold` is the **only path** that creates rows. The webhook never inserts. One row is persisted synchronously by `HoldFundsCommand` with these columns:

| Column | Type | Source | Example |
|---|---|---|---|
| `Id` | `int` PK identity | DB-generated | `42` |
| `ClientEmail` | `text` required | Request body | `client@example.com` |
| `ConsultantEmail` | `text` required | Request body | `consultant@example.com` |
| `Amount` | `numeric(18,4)` required | Request body | `50.0000` |
| `ServiceDescription` | `text` required | Request body | `"Webhook correlation test"` |
| `Status` | `varchar(50)` required | Lifecycle | `"Held"` after auth (then `"Released"` / `"Disputed"`) |
| `ExternalReference` | `text?` unique-filtered | Stripe `PaymentIntent.id` | `pi_3TS4sZ...` |
| `ExternalProvider` | `text?` | Strategy resolver | `"Stripe"` |
| `DisputeReason` | `text?` | Dispute command | `null` until disputed |
| `PlatformFee` | `numeric` | Computed at creation (1.5%) | `0.7500` |
| `PlatformFeePercentage` | `numeric` | Snapshot of rate | `0.0150` |
| `CreatedAt` | `timestamp UTC` | `DateTime.UtcNow` at creation | `2026-04-30T20:24:28Z` |

Source of truth: `EscrowApp/Models/EscrowTransaction.cs` and `EscrowApp/Data/Configurations/EscrowTransactionConfiguration.cs` (unique filtered index on `ExternalReference` enables idempotent webhook lookup).

#### What `PaymentIntentEventHandler` does and does NOT do

`Features/Escrow/Webhooks/PaymentIntentEventHandler.cs` is the MediatR notification handler for `payment_intent.succeeded`, dispatched by `StripeWebhookEndpoint` after signature verification.

**Does:**
- Looks up the `EscrowTransaction` by `ExternalReference == pi_...` via `IEscrowTransactionRepository.GetByExternalReferenceAsync`.
- Validates `Status ∈ {Pending, Held}`, the amount in cents matches `transaction.Amount × 100`, and `ExternalProvider == "Stripe"`.
- Publishes `PaymentReceivedEvent` to the in-process `IEventBus` for downstream listeners (email confirmations, dashboard refresh, future release automation).
- Logs every branch (`✅` confirmed, `⚠️` unknown PI / wrong status / wrong provider, `❌` amount mismatch / unexpected error) and **swallows all exceptions** so the webhook always returns `204` to Stripe.

**Does NOT:**
- ❌ Insert any DB row — the row must already exist from `POST /api/escrow/hold`.
- ❌ Mutate `Status`, `ExternalReference`, or any other field. Status stays `"Held"` (MVP behavior — webhook is observational only).
- ❌ Call `SaveChangesAsync` or any repository write method.
- ❌ Throw on missing/invalid transactions — unknown `pi_...` events are logged and ignored to keep the webhook idempotent and Stripe retries quiet.
- ❌ Capture, release, or refund funds — those are explicit operator actions via the release/cancel commands.

> **Implication for testing:** triggering `stripe trigger payment_intent.succeeded` **without** first calling `POST /api/escrow/hold` produces a synthetic `pi_...` that does not exist in your DB. The handler will log `⚠️ Webhook received for unknown PaymentIntent — ignoring` and the table stays empty. To exercise the success branch end-to-end, always run Step 5 first, then resend the matching event in Step 6.

### Step 6 — Correlation test (`stripe events resend`)

Replays a **real** event tied to your `pi_...` so the handler should find the matching `EscrowTransaction`:

```bash
stripe events resend evt_xxx_from_step_5
```

**Verify (success):**

- `<-- [204]` from the endpoint.
- App logs show:
  1. `✅ Webhook verified and parsed`
  2. `📨 Dispatching PaymentIntentSucceeded to MediatR`
  3. The downstream handler **finds the matching transaction** (no "unknown PaymentIntent" warning).
  4. The handler publishes its follow-on domain event without throwing.
- DB state: the transaction associated with `pi_...` is unchanged in shape (no duplicates, no orphan rows).

### Step 7 — Capture evidence

Record before closing terminals:

- Exact `stripe listen` and `stripe trigger`/`events resend` commands used.
- The endpoint URL (HTTP vs HTTPS).
- HTTP status returned for each delivery.
- Relevant log lines (event id, payment intent id, dispatch confirmation).
- Whether correlation in Step 6 succeeded.

---

## Tracking events in the Stripe Dashboard

The Stripe CLI and the Dashboard's webhook UI are **two different surfaces**. Knowing which one shows what avoids hours of confusion.

| Capability | Stripe CLI (`stripe listen`) | Dashboard → Developers → Webhooks |
|---|---|---|
| Listed as a registered endpoint | ❌ No | ✅ Yes |
| Events visible in **Developers → Events (test)** | ✅ Yes | ✅ Yes |
| Signing secret (`whsec_...`) | Ephemeral — rotates each session | Permanent per endpoint |
| Needs a publicly reachable URL | ❌ No (CLI tunnels to localhost) | ✅ Yes (tunnel or deployed) |
| Works with Dashboard's **"Send test webhook"** button | ❌ No | ✅ Yes |

### What you CAN see in the Dashboard while running this guide

Even though `stripe listen` does not register an endpoint, every event it forwards is a real test-mode event on your Stripe account. Open [Dashboard → Developers → Events (test)](https://dashboard.stripe.com/test/events) and you will see:

- The `evt_...` triggered in **Step 4** (`stripe trigger payment_intent.succeeded`).
- The `evt_...` resent in **Step 6** (`stripe events resend`), with a new delivery attempt logged.
- Click any event → **Webhooks** tab to inspect the JSON payload, delivery target, HTTP status, and response time the CLI forwarded.

This is the easiest way to grab an `evt_...` id for Step 6 or to confirm that a `payment_intent.succeeded` was actually emitted for the `pi_...` you created in Step 5.

### What you CANNOT do from the Dashboard against localhost

The Dashboard's **"Send test webhook"** button (under Developers → Webhooks → `<endpoint>` → "Send test webhook") only targets endpoints **registered** in that panel. It cannot reach `http://localhost:5093` because Stripe's servers cannot route to your machine.

### Mirroring this flow from the Dashboard

If you specifically need to test the **Dashboard webhook UI** (e.g. validating a permanent `whsec_...`, testing retry behavior, or simulating a production delivery), use one of:

#### Option 1 — Local + public tunnel

1. Start a tunnel to `http://localhost:5093` using ngrok, Cloudflare Tunnel, or similar:
   ```bash
   ngrok http 5093
   ```
2. In Dashboard → Developers → Webhooks → **Add endpoint**, register the public URL:
   `https://<your-tunnel>.ngrok-free.app/api/webhooks/stripe`
3. Subscribe to `payment_intent.succeeded` (and any others your handler dispatches).
4. Copy the endpoint's permanent `whsec_...` and apply it via Step 2 of this guide (`dotnet user-secrets set "Stripe:Webhook:EndpointSecret" "whsec_..."`). Restart the app.
5. Use the Dashboard's **"Send test webhook"** button on that endpoint, or **"Resend"** on any past event.

> ⚠️ The tunnel-issued `whsec_...` is **different** from the one printed by `stripe listen`. Pick one mode at a time — do not run both against the same app instance, or signature verification will fail intermittently as the active secret changes.

#### Option 2 — Deployed staging environment

1. Deploy the app to a publicly reachable staging host (HTTPS required by Stripe).
2. Register that host's `/api/webhooks/stripe` URL in Dashboard → Developers → Webhooks.
3. Store the permanent `whsec_...` as a managed secret (Key Vault / app settings) — never commit it.
4. Use the Dashboard "Send test webhook" / "Resend" buttons against the staging endpoint.

This is the closest local-free equivalent to the production webhook path and is the recommended pre-release smoke test.

> ⚠️ **Compliance reminder:** Even staging webhook validation is a compliance-sensitive payment flow. See the **Compliance Note** at the end of this document.

---

## Response Code Reference

Source: `EscrowApp/Infrastructure/Webhooks/Stripe/StripeWebhookEndpoint.cs`

| Code | Meaning | Likely cause |
|---|---|---|
| `204 No Content` | Success — event verified and dispatched | Normal flow |
| `400 Bad Request` | Empty body or missing `Stripe-Signature` header | Caller is not Stripe CLI / not posting raw event JSON |
| `401 Unauthorized` | Signature verification failed | `Stripe:Webhook:EndpointSecret` does not match the active `whsec_...` |
| `500 Internal Server Error` | Unhandled exception in dispatch | Inspect logs; Stripe will retry |

## Success Criteria

The manual test passes when:

1. Stripe CLI forwards events without TLS or connectivity errors.
2. `stripe trigger payment_intent.succeeded` returns **`204`** with logs showing `Webhook verified and parsed`.
3. `stripe events resend <evt_id>` for a real app-created PI also returns **`204`** and logs show the matching transaction was found.
4. No `401` responses occur after Step 2.
5. No duplicate or malformed `EscrowTransaction` rows are created.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Stripe CLI shows `[307]` for every event, no app logs | Forwarding to `http://localhost:5093` — `UseHttpsRedirection()` 307-redirects to `https://localhost:7037`, and Stripe CLI does not follow redirects | Restart `stripe listen` with `--forward-to https://localhost:7037/api/webhooks/stripe` (add `--skip-verify` if dev cert is not trusted) |
| `401 Unauthorized` on every delivery |`Stripe:Webhook:EndpointSecret` ≠ the `whsec_...` from the active `stripe listen` | Re-copy the secret from Step 1 and re-apply via Step 2; restart the app |
| `400 Missing Stripe-Signature header` | Hitting the endpoint with curl/Postman without the header | Use Stripe CLI to generate signed deliveries |
| Logs show `unknown PaymentIntent` after Step 4 | Expected for `stripe trigger` (new synthetic PI) | Move on to Steps 5–6 for correlation |
| Stripe CLI cannot reach the endpoint | App not listening on the URL passed to `--forward-to` | Confirm `launchSettings.json` profile, check `GET /api/webhooks/stripe` returns the diagnostic JSON |
| HTTPS forwarding fails with TLS error | Local dev cert not trusted | Run `dotnet dev-certs https --trust`, or pass `--skip-verify` to `stripe listen`, or fall back to HTTP |
| App reads stale secret after rotating `stripe listen` | Cached `IOptions<StripeWebhookOptions>` snapshot | Restart the app after each new `stripe listen` session |
| `whsec_test_secret` appears in logs | App is using the placeholder from `appsettings.json` | Apply Step 2 — the placeholder is not a valid signing secret |

---

## Compliance Note

This is a compliance-sensitive payment/webhook test flow. Any production rollout or user-facing process derived from this guide still requires legal review before release. See `docs/business/business-model/strategic-plan.md` under **"Why payment and webhook test guidance still requires legal review"** for the governing rationale.
