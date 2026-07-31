# QF-014 — Empty `ApiKeys:dev-client-01:Key` in `appsettings.Development.json` → 401 on Every Call

**Date:** 2026-05-01
**Layer / Concern:** Infrastructure — Configuration / Secrets management
**Severity:** 🟠 No request can authenticate; surface looks identical to "wrong key"

---

## Symptom

After QF-012 (scheme pin) and QF-013 (antiforgery), `POST /api/escrow/hold` with any `X-Api-Key` value returned **401 Unauthorized**, even when the value matched what the developer expected.

Inspecting `appsettings.Development.json`:

```json
"ApiKeys": {
  "dev-client-01": {
    "Key": "",
    "ClientName": "Development Client",
    "Scopes": [ "escrow:write", "escrow:read" ]
  }
}
```

— `Key` is the empty string. The handler compares the incoming header against `""` and rejects every non-empty value.

---

## Root Cause

The committed `appsettings.Development.json` ships with a placeholder (`""`) on purpose: **secrets must never be checked into source**. The handler does not log "key is empty" — it just returns the standard 401, so the failure mode is indistinguishable from "wrong key".

---

## Fix

Set the value via **dotnet user-secrets** (per-developer, not committed):

```powershell
cd EscrowApp
dotnet user-secrets set "ApiKeys:dev-client-01:Key" "ntzt_dev_k1_<random-32-bytes>"
```

User-secrets are stored under `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows and override the `appsettings*.json` value at runtime in Development.

### Generating a strong key

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
```

Or any URL-safe base64 / hex value of ≥32 bytes.

---

## Verification

```powershell
# 1. Confirm it's set
dotnet user-secrets list | Select-String "ApiKeys"

# 2. Send a request
curl -X POST https://localhost:7037/api/escrow/hold `
  -H "X-Api-Key: <the-value-you-just-set>" `
  -H "Content-Type: application/json" `
  -d '{...}'
```

Expected: `200 OK` (or downstream validation error), not 401.

---

## Production note

In production, the same key is loaded from environment variables or Azure Key Vault — never `appsettings.Production.json`. The empty placeholder in the committed file is **the intended state** for the repo.

---

## See also

- [QF-012 ApiAccess policy missing scheme pin](12_ApiAccess_Policy_Missing_Scheme_Pin_INFRASTRUCTURE_AUTH.md)
- [QF-015 Swagger global Authorize button for API key](15_Swagger_Global_Authorize_Button_For_ApiKey_PRESENTATION_DOCS.md)
- [QF-017 Stripe placeholder secret key](17_Stripe_Placeholder_SecretKey_UserSecrets_INFRASTRUCTURE_PAYMENTS.md)
