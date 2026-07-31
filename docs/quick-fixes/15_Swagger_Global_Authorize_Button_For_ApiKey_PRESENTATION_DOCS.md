# QF-015 — Swagger UI Has No Per-Endpoint `X-Api-Key` Field; Use the Global Authorize Modal

**Date:** 2026-05-01
**Layer / Concern:** Presentation — API documentation (Swagger UI / OpenAPI)
**Severity:** 🟢 Documentation / UX — not a defect, but blocks first-time API consumers

---

## Symptom

> "I am testing through Swagger and there is no field to insert `X-Api-Key`."

The user clicks **Try it out** on `POST /api/escrow/hold`, fills in the JSON body, hits **Execute**, and gets a 401 because no `X-Api-Key` header is sent. There is no obvious place inside the operation panel to enter it.

---

## Root Cause

This is **expected Swagger UI behavior** for security schemes registered globally. When the OpenAPI document declares an `apiKey`-type security requirement, the SwashBuckle UI surfaces it through a single **🔓 Authorize** button at the top of the page — **not** as an inline field per endpoint.

Once a value is entered in the Authorize modal, it is automatically attached to every request the UI sends, including subsequent **Try it out** invocations across all endpoints.

---

## Fix

This is a workflow correction, not a code fix.

### Steps to authorize in Swagger

1. Open `https://localhost:7037/swagger`.
2. Click the green **🔓 Authorize** button in the upper-right corner of the page.
3. In the modal that appears, locate the `ApiKey` (or `X-Api-Key`) section.
4. Paste the value (from `dotnet user-secrets`, see [QF-014](14_ApiKey_Config_Empty_UseUserSecrets_INFRASTRUCTURE_AUTH.md)).
5. Click **Authorize**, then **Close**.
6. The header is now attached to every subsequent request from Swagger UI.

### Verifying

After Authorizing, open any operation, click **Try it out** → **Execute**, and inspect the **Curl** preview. It should include:

```
-H 'X-Api-Key: <your-key>'
```

If the header is absent, the Authorize modal value was never saved — repeat steps 2-5.

---

## Optional: surface scheme description more prominently

If the team finds new developers consistently miss the button, the SwaggerGen registration in `Program.cs` can include richer `Description` text on the `OpenApiSecurityScheme`. This text appears inside the Authorize modal and helps signal the intent.

---

## See also

- [QF-014 Empty ApiKey config — use user-secrets](14_ApiKey_Config_Empty_UseUserSecrets_INFRASTRUCTURE_AUTH.md)
- [QF-012 ApiAccess policy missing scheme pin](12_ApiAccess_Policy_Missing_Scheme_Pin_INFRASTRUCTURE_AUTH.md)
