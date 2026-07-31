# copu 09 — REST API Integration Layer

**Status:** Implemented (MVP)

## Overview

The NexTruzt.io REST API enables third-party backends (e-commerce, marketplaces, SaaS)
to integrate secure payment holding directly into their systems. The API wraps the
existing MediatR application layer behind authenticated HTTP endpoints.

> **Compliance-sensitive note:** If this document is adapted for external partners, use
> approved user-facing terminology such as **secure payment holding** or **held funds**.
> Do not imply that NexTruzt.io is a licensed escrow agent.

## MVP Integration Stance

- The primary public mutation entry point is **`POST /api/escrow/hold`**.
- That endpoint performs an **atomic create-and-hold** in one call.
- There is **no public `POST /api/escrow/{id}/hold` endpoint** in the current MVP controller.
  `HoldFunds` exists as an application slice, but it is not exposed as a public REST operation.
- The MVP integration model is **server-to-server only**.
- The MVP does **not** provide outbound partner webhooks yet; integrators should poll
  `GET /api/escrow/{id}` or `GET /api/escrow`.
- URI versioning is **not introduced yet**. The current unversioned `/api/escrow/*`
  surface is the single supported MVP contract.

## Architecture

```
Third-Party Platform
        │
        ▼ HTTP + X-Api-Key
┌───────────────────────────┐
│   EscrowController        │  ← API presentation layer
│   (REST endpoints)        │
├───────────────────────────┤
│   MediatR Pipeline        │  ← Application layer shared with Blazor UI
│   Commands / Queries      │
├───────────────────────────┤
│   Repository + Strategies │  ← Infrastructure layer
│   EF Core + Stripe SDK    │
└───────────────────────────┘
```

## Environments and Base URLs

| Environment | Base URL | Notes |
|---|---|---|
| Local HTTP | `http://localhost:5093` | Default local API endpoint from `launchSettings.json` |
| Local HTTPS | `https://localhost:7037` | Optional local HTTPS endpoint when local cert trust is configured |
| Hosted non-local | Deployment-specific | Configure via environment variable or secrets; do not hardcode example domains in your client |

> **MVP note:** The repository does not currently define a separate hosted sandbox URL.
> Use local development or an explicitly provisioned non-production deployment for integration testing.

## Authentication and Onboarding

API key authentication uses the `X-Api-Key` header on every request.

```bash
curl -X GET http://localhost:5093/api/escrow \
  -H "X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4"
```

### Onboarding flow

1. Obtain an API key from the NexTruzt admin.
2. Store the API key in your server-side secret store.
3. Configure the environment-specific base URL.
4. Send `X-Api-Key` on every request.
5. Send a deterministic `X-Idempotency-Key` on every mutation request.

Example key handoff:

```text
Client ID:  your-platform-name
API Key:    ntzt_prod_k1_xxxxxxxxxxxxxxxx
```

### Security model

- `ApiKeyAuthenticationHandler` validates the incoming key
- Successful auth creates a `ClaimsPrincipal` with `api_client_id` claim
- `[Authorize(Policy = "ApiAccess")]` guards all controller endpoints
- `RaisedBy` on disputes and `CancelledBy` on cancellations are derived from the authenticated identity

## MVP Public Integration Contract

| Operation | Method + Path | Request shape | Response shape | Notes |
|---|---|---|---|---|
| Create and hold | `POST /api/escrow/hold` | `CreateAndHoldRequest` | `EscrowTransactionResponse` | Primary Day-1 write path; creates the transaction and places the hold atomically |
| Get by ID | `GET /api/escrow/{id}` | None | `EscrowTransactionResponse` | Poll a single transaction state |
| List | `GET /api/escrow?page=1&pageSize=20&status=...` | Query params | `PaginatedResponse<EscrowTransactionResponse>` | Supports optional `status` filter |
| Release | `POST /api/escrow/{id}/release` | Header only (`X-Idempotency-Key`) | `ReleaseFundsResult` | Releases held funds |
| Dispute | `POST /api/escrow/{id}/dispute` | `DisputeFundsApiRequest` | `DisputeFundsResult` | Cancels the hold and marks the transaction disputed |
| Cancel | `POST /api/escrow/{id}/cancel` | `CancelFundsApiRequest` | `CancelFundsResult` | Cooperative cancellation path |

## Request / Response Examples

### Create and hold funds

```http
POST /api/escrow/hold
X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4
X-Idempotency-Key: order-123
Content-Type: application/json

{
  "clientEmail": "client@example.com",
  "consultantEmail": "consultant@example.com",
  "amount": 500.00,
  "serviceDescription": "Tax Consulting Q1 2026",
  "paymentMethodId": "pm_card_visa",
  "providerName": "Stripe"
}
```

**Response: 201 Created**

```json
{
  "id": 1,
  "clientEmail": "client@example.com",
  "consultantEmail": "consultant@example.com",
  "amount": 500.00,
  "serviceDescription": "Tax Consulting Q1 2026",
  "status": "Funded (Held)",
  "externalReference": "pi_3abc...",
  "externalProvider": "Stripe",
  "platformFee": 7.50,
  "platformFeePercentage": 0.015,
  "totalCharged": 507.50,
  "createdAt": "2026-04-04T21:00:00Z"
}
```

### Release held funds

```http
POST /api/escrow/123/release
X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4
X-Idempotency-Key: release-123
```

**Response: 200 OK**

```json
{
  "transactionId": 123,
  "status": "Completed (Released)",
  "success": true
}
```

### List transactions

```http
GET /api/escrow?page=1&pageSize=10&status=Funded%20(Held)
X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4
```

### Validation failure

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Amount": ["'Amount' must be greater than '0'."],
    "ClientEmail": ["Client and consultant cannot be the same person."]
  }
}
```

> **Validation note:** All POST endpoints validate input before handler execution.
> Validation errors return 400 with errors grouped by property name.

## Error Handling

All API errors return RFC 7807-compatible `ProblemDetails`.

```json
{
  "status": 422,
  "title": "Business Rule Violation",
  "detail": "Transaction 5 must be in 'Funded (Held)' status to release. Current: 'Pending'.",
  "instance": "/api/escrow/5/release",
  "type": "https://httpstatuses.com/422"
}
```

| Status | When |
|---|---|
| 400 | Validation failed (invalid body or rule input) |
| 401 | Missing or invalid API key |
| 403 | Authenticated but not allowed |
| 404 | `GET /api/escrow/{id}` did not find the resource |
| 422 | Business rule violation, including wrong state and mutation lookups that fail in handlers |
| 500 | Unexpected server error (details hidden) |

## Swagger / OpenAPI

Swagger is available at `/swagger` in Development only. The local OpenAPI document is:

```text
/swagger/v1/swagger.json
```

> **MVP note:** The OpenAPI document is generated from the running app and is not yet
> published as a separately versioned partner artifact.

For the full Swagger-discovered route inventory, including minimal app endpoints and the Stripe webhook routes, see [API endpoint reference](api-endpoint-reference.md).

## Third-Party Integration Guide

### How it works

NexTruzt.io acts as secure payment holding middleware: your platform owns the user
experience and business workflow, while NexTruzt owns payment hold, release, dispute,
and cancellation operations.

```
┌─────────────────────┐          ┌────────────────────────┐          ┌─────────┐
│ Your platform       │  REST    │ NexTruzt.io API        │  Stripe  │ Bank /  │
│ (e-commerce, SaaS)  │────────▶│ /api/escrow/*          │────────▶│ card    │
│ backend only        │◀────────│ JSON responses         │◀────────│ network │
└─────────────────────┘          └────────────────────────┘          └─────────┘
```

### Happy path

Your platform orchestrates the following flow:

```text
Client places order
        │
        ▼
POST /api/escrow/hold
        │
        ├─ creates transaction
        ├─ authorizes payment
        └─ returns transactionId + status "Funded (Held)"
        │
        ▼
Store transactionId in your system
        │
        ▼
Consultant delivers service
        │
        ▼
POST /api/escrow/{id}/release
        │
        └─ returns status "Completed (Released)"
```

### Alternate paths

- **Dispute:** `POST /api/escrow/{id}/dispute`
- **Cancel:** `POST /api/escrow/{id}/cancel`
- **Status polling:** `GET /api/escrow/{id}` or `GET /api/escrow`

> **Important nuance:** There is no public `POST /api/escrow/{id}/hold` endpoint in the MVP.
> If your workflow needs a two-step create-then-hold process, that is a post-MVP enhancement.

## Implementation Examples

Use a configurable base URL rather than hardcoding an example production hostname.

#### Node.js / Express

```javascript
const NEXTRUZT_API = process.env.NEXTRUZT_BASE_URL;
const API_KEY = process.env.NEXTRUZT_API_KEY;

async function createHold(order) {
  const res = await fetch(`${NEXTRUZT_API}/api/escrow/hold`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY,
      'X-Idempotency-Key': `order-${order.id}`
    },
    body: JSON.stringify({
      clientEmail: order.clientEmail,
      consultantEmail: order.vendorEmail,
      amount: order.total,
      serviceDescription: order.description,
      paymentMethodId: order.stripePaymentMethodId,
      providerName: 'Stripe'
    })
  });

  if (!res.ok) {
    throw new Error(`Create hold failed: ${res.status}`);
  }

  return res.json();
}

async function releaseFunds(transactionId) {
  const res = await fetch(`${NEXTRUZT_API}/api/escrow/${transactionId}/release`, {
    method: 'POST',
    headers: {
      'X-Api-Key': API_KEY,
      'X-Idempotency-Key': `release-${transactionId}`
    }
  });

  if (!res.ok) {
    throw new Error(`Release failed: ${res.status}`);
  }

  return res.json();
}
```

#### Python / FastAPI

```python
import httpx
import os

API_KEY = os.environ["NEXTRUZT_API_KEY"]
BASE_URL = os.environ["NEXTRUZT_BASE_URL"]
HEADERS = {"X-Api-Key": API_KEY}

async def create_hold(order: dict) -> dict:
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{BASE_URL}/api/escrow/hold",
            json={
                "clientEmail": order["client_email"],
                "consultantEmail": order["vendor_email"],
                "amount": order["total"],
                "serviceDescription": order["description"],
                "paymentMethodId": order["stripe_pm_id"],
                "providerName": "Stripe",
            },
            headers={**HEADERS, "X-Idempotency-Key": f"order-{order['id']}"},
        )
        response.raise_for_status()
        return response.json()

async def release_funds(transaction_id: int) -> dict:
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{BASE_URL}/api/escrow/{transaction_id}/release",
            headers={**HEADERS, "X-Idempotency-Key": f"release-{transaction_id}"},
        )
        response.raise_for_status()
        return response.json()
```

#### C# / HttpClient

```csharp
public sealed class NexTruztClient(HttpClient http)
{
    public async Task<EscrowTransactionResponse> CreateHoldAsync(
        CreateAndHoldRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/escrow/hold")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-Idempotency-Key", idempotencyKey);

        using var response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EscrowTransactionResponse>(cancellationToken: ct))!;
    }

    public async Task<ReleaseFundsResult> ReleaseFundsAsync(
        int transactionId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/escrow/{transactionId}/release");
        message.Headers.Add("X-Idempotency-Key", idempotencyKey);

        using var response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReleaseFundsResult>(cancellationToken: ct))!;
    }
}

builder.Services.AddHttpClient<NexTruztClient>(client =>
{
    client.BaseAddress = new Uri(configuration["NexTruzt:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Api-Key", configuration["NexTruzt:ApiKey"]);
});
```

## Integration Checklist

- [ ] Obtain an API key from NexTruzt admin
- [ ] Configure `NEXTRUZT_BASE_URL` (or equivalent secret-backed config)
- [ ] Add `X-Api-Key` to every request
- [ ] Add deterministic `X-Idempotency-Key` values to all mutation requests
- [ ] Call `POST /api/escrow/hold` for the atomic create-and-hold workflow
- [ ] Store `transactionId` from the hold response in your own database
- [ ] Use `GET /api/escrow/{id}` or `GET /api/escrow` for status polling
- [ ] Implement release, dispute, and cancel flows as needed
- [ ] Handle `400`, `401`, `403`, `404`, `422`, and `500` responses explicitly
- [ ] Test the full lifecycle locally or in an assigned non-production environment

## Key Files

| File | Purpose |
|---|---|
| `Features/Escrow/Api/EscrowController.cs` | Public REST controller |
| `Features/Escrow/Api/ApiContracts.cs` | Request/response contracts |
| `Features/Escrow/CreateAndHoldFunds/` | Atomic create-and-hold slice |
| `Features/Escrow/ReleaseFunds/` | Release held funds |
| `Features/Escrow/DisputeFunds/` | Dispute flow |
| `Features/Escrow/CancelFunds/` | Cooperative cancellation flow |
| `Features/Escrow/GetTransaction/` | Query handler for GET by ID |
| `Features/Escrow/ListTransactions/` | Paginated query handler |
| `Infrastructure/Auth/ApiKeyAuthenticationHandler.cs` | API key validation |
| `Infrastructure/Middleware/ApiExceptionMiddleware.cs` | `ProblemDetails` error handling |

## Deferred Beyond MVP

- [ ] Dedicated public `POST /api/escrow/{id}/hold` endpoint for a true two-step partner workflow
- [ ] JWT Bearer auth for SPA/mobile clients
- [ ] Multi-tenant isolation (`TenantId` on transactions)
- [ ] Outbound partner webhooks for domain events
- [ ] Rate limiting on mutation endpoints
- [ ] SDK generation (C#, JavaScript, Python)
