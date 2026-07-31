# QF-017 — `Stripe.StripeException: Invalid API Key provided: sk_test_***y123`

**Date:** 2026-05-02
**Layer / Concern:** Infrastructure — Payments (`StripePaymentStrategy`), Configuration / Secrets
**Severity:** 🔴 No payment hold can be authorized; every call to Stripe fails before a `PaymentIntent` is created

---

## Symptom

After all auth, antiforgery, and schema fixes, `POST /api/escrow/hold` reached the strategy and threw:

```
Stripe.StripeException: Invalid API Key provided: sk_test_***************y123
   at Stripe.LiveApiRequestor.ProcessResponse[T](StripeResponse, ApiMode)
   at EscrowApp.Services.Strategies.StripePaymentStrategy.HoldFundsAsync(...)
        in StripePaymentStrategy.cs:line 35
```

The `***y123` ending is Stripe's redacted echo of the key it received — confirming a placeholder/test-fixture value (e.g. `sk_test_xxx...y123`) was pulled from configuration.

---

## Root Cause

`Stripe:SecretKey` was either unset or set to a placeholder string in the developer's configuration. The committed `appsettings*.json` deliberately does **not** contain a real key (secrets must never be checked into source). When the handler started, it bound that placeholder into `StripeSettings.SecretKey` and the Stripe SDK rejected it on the first API call.

---

## Fix

You need a real **Stripe test-mode secret key** (starts with `sk_test_`). Get it from:

> Stripe Dashboard → Developers → API keys → Secret key (Test mode) → <https://dashboard.stripe.com/test/apikeys>

Then store it via **dotnet user-secrets** (never in `appsettings*.json`):

```powershell
cd EscrowApp
dotnet user-secrets set "Stripe:SecretKey" "sk_test_<your-real-test-key>"
```

While you're there, also set the webhook secret if you'll be testing webhooks:

```powershell
dotnet user-secrets set "Stripe:Webhook:EndpointSecret" "whsec_<from-stripe-cli-listen>"
```

Get the webhook secret from the output of `stripe listen --forward-to https://localhost:7037/api/webhooks/stripe`:

```
> Ready! You are using Stripe API Version [...]
> Your webhook signing secret is whsec_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx (^C to quit)
```

---

## Verification

```powershell
# 1. Confirm both secrets are set
dotnet user-secrets list | Select-String "Stripe"

# Expected:
# Stripe:SecretKey = sk_test_...
# Stripe:Webhook:EndpointSecret = whsec_...

# 2. Restart the app to reload configuration
# 3. POST /api/escrow/hold
curl -X POST https://localhost:7037/api/escrow/hold `
  -H "X-Api-Key: <dev-key>" `
  -H "Content-Type: application/json" `
  -H "X-Idempotency-Key: ntzt_dev_k1_<unique>" `
  -d '{
    "clientEmail": "client@example.test",
    "consultantEmail": "consultant@example.test",
    "amount": 50.00,
    "serviceDescription": "Smoke test",
    "paymentMethodId": "pm_card_visa"
  }'
```

Expected response: `200 OK` with the persisted transaction including `ExternalReference: "pi_..."` and `Status: "Held"`. In the Stripe Dashboard → Payments (Test mode), a new requires_capture / authorized PaymentIntent should appear.

---

## Production note

In production, `Stripe:SecretKey` is provisioned via environment variables or Azure Key Vault — never user-secrets and never `appsettings.Production.json`. The Live key (`sk_live_...`) and Test key (`sk_test_...`) must never be mixed.

---

## See also

- [QF-011 Stripe webhook 307 redirect & 422 ILogger DI](11_Stripe_Webhook_307_Redirect_422_ILogger_DI_INFRASTRUCTURE_WEBHOOKS.md)
- [QF-014 Empty ApiKey config — use user-secrets](14_ApiKey_Config_Empty_UseUserSecrets_INFRASTRUCTURE_AUTH.md)
