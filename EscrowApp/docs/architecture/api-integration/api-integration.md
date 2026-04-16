#copu 09 — REST API Integration Layer

**Status:** Implemented (MVP)

## Overview

The NexTruzt.io REST API enables third-party platforms (e-commerce, marketplaces, SaaS) to
integrate escrow functionality directly into their systems. The API wraps existing MediatR
commands behind authenticated HTTP endpoints.

## Architecture

```
Third-Party Platform
        │
        ▼ HTTP + X-Api-Key
┌───────────────────────────┐
│   EscrowController        │  ← API Presentation Layer
│   (REST endpoints)        │
├───────────────────────────┤
│   MediatR Pipeline        │  ← Application Layer (shared with Blazor UI)
│   Commands / Queries      │
├───────────────────────────┤
│   Repository + Strategies │  ← Infrastructure Layer
│   EF Core + Stripe SDK    │
└───────────────────────────┘
```

## Endpoints

| Method | Path                        | Description                          |
|--------|-----------------------------|--------------------------------------|
| POST   | `/api/escrow/hold`          | Create transaction + hold funds      |
| GET    | `/api/escrow/{id}`          | Get transaction by ID                |
| GET    | `/api/escrow`               | List transactions (paginated)        |
| POST   | `/api/escrow/{id}/release`  | Release held funds                   |
| POST   | `/api/escrow/{id}/dispute`  | Raise dispute (cancels hold)         |
| POST   | `/api/escrow/{id}/cancel`   | Cancel escrow by mutual agreement    |

## Authentication

API Key authentication via `X-Api-Key` header.

```bash
curl -X GET http://localhost:5093/api/escrow \
  -H "X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4"
```

Keys are configured in `appsettings.Development.json` under the `ApiKeys` section.
Each key maps to a client identity with claims.

### Security Model

- `ApiKeyAuthenticationHandler` validates the header against configured keys
- Successful auth creates a `ClaimsPrincipal` with `api_client_id` claim
- `[Authorize(Policy = "ApiAccess")]` on all controller endpoints
- `RaisedBy` on disputes is derived from the authenticated identity — not from the request body

## Request/Response Examples

### Create and Hold Funds

```http
POST /api/escrow/hold
X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4
X-Idempotency-Key: unique-request-id-123
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
  "createdAt": "2026-04-04T21:00:00Z"
}
```

**Response: 400 Bad Request (Validation Error)**
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Amount": ["Escrow amount must be greater than zero."],
    "IdempotencyKey": ["Idempotency key is required."],
    "ClientEmail": ["Client and consultant cannot be the same person."]
  }
}
```

> **Note:** All POST endpoints (`/hold`, `/release`, `/dispute`, `/cancel`) validate input before handler execution. Validation failures always return 400 with grouped errors (errors grouped by property name for client-side mapping).

### List Transactions (Paginated)

```http
GET /api/escrow?page=1&pageSize=10&status=Funded%20(Held)
X-Api-Key: ntzt_dev_k1_a3b9f7e2d1c4
```

## Error Handling

All API errors return RFC 7807 ProblemDetails:

```json
{
  "status": 422,
  "title": "Business Rule Violation",
  "detail": "Transaction 5 not found.",
  "instance": "/api/escrow/hold",
  "type": "https://httpstatuses.com/422"
}
```

| Status | When                                    |
|--------|-----------------------------------------|
| 400    | Validation failed (invalid input)       |
| 401    | Missing or invalid API key              |
| 404    | Transaction not found                   |
| 422    | Business rule violation (wrong state)   |
| 500    | Unexpected server error (details hidden)|

> **Validation errors (400):** Grouped by property name for client-side mapping. See example above.

## Swagger / OpenAPI

Available at `/swagger` in Development environment only.
OpenAPI spec: `/swagger/v1/swagger.json`

## Key Files

| File | Purpose |
|------|---------|
| `Features/Escrow/Api/EscrowController.cs` | REST controller |
| `Features/Escrow/Api/ApiContracts.cs` | Request/response DTOs |
| `Features/Escrow/CreateAndHoldFunds/` | New MediatR command + handler |
| `Features/Escrow/CancelFunds/` | Cancel escrow MediatR command + handler (stub) |
| `Features/Escrow/GetTransaction/` | Query handler for GET by ID |
| `Features/Escrow/ListTransactions/` | Paginated query handler |
| `Infrastructure/Auth/ApiKeyAuthenticationHandler.cs` | API key validation |
| `Infrastructure/Middleware/ApiExceptionMiddleware.cs` | ProblemDetails error handler |

## Future Enhancements

- [ ] JWT Bearer auth for SPA/mobile clients
- [ ] Multi-tenant isolation (`TenantId` on transactions)
- [ ] Webhook delivery for domain events
- [ ] Rate limiting on mutation endpoints
- [ ] SDK generation (C#, JavaScript, Python)

---

## Third-Party Integration Guide

### How It Works

NexTruzt.io acts as a **payment escrow middleware** — your platform handles the user
experience, and NexTruzt holds/releases money on your behalf. The integration is
purely server-to-server via REST.

```
┌─────────────────────┐          ┌──────────────────────┐          ┌─────────┐
│  Your Platform       │  REST    │  NexTruzt.io API     │  Stripe  │  Bank   │
│  (e-commerce, SaaS)  │────────▶│  /api/escrow/*       │────────▶│  $$$    │
│                      │◀────────│                      │◀────────│         │
└─────────────────────┘  JSON    └──────────────────────┘  SDK     └─────────┘
```

### Step 1 — Obtain an API Key

Request an API key from the NexTruzt admin. You'll receive:

```
Client ID:  your-platform-name
API Key:    ntzt_prod_k1_xxxxxxxxxxxxxxxx
```

Include it in every request:

```
X-Api-Key: ntzt_prod_k1_xxxxxxxxxxxxxxxx
```

### Step 2 — Escrow Lifecycle (Happy Path)

Your platform orchestrates the following 3-call flow:

```
Client places order → Your backend calls NexTruzt
                                │
                      ┌─────────▼──────────┐
                      │  POST /hold        │  Funds captured from client's card
                      │  Status: "Funded   │  and held in escrow.
                      │  (Held)"           │
                      └─────────┬──────────┘
                                │
                      Consultant delivers service
                                │
                      ┌─────────▼──────────┐
                      │  POST /{id}/release │  Client confirms delivery.
                      │  Status: "Completed │  Funds released to consultant.
                      │  (Released)"        │
                      └────────────────────┘
```

**Dispute path** — If the client is unsatisfied:

```
                      ┌─────────────────────┐
                      │  POST /{id}/dispute  │  Hold cancelled, funds returned
                      │  Status: "Disputed"  │  to client automatically.
                      └─────────────────────┘
```

**Cancel path** — If both parties agree to void the escrow (cooperative):

```
                      ┌─────────────────────┐
                      │  POST /{id}/cancel   │  Hold voided, funds returned
                      │  Status: "Cancelled" │  to client by mutual agreement.
                      └─────────────────────┘
```

> **Dispute vs Cancel:** Dispute is a *contested* action — one party objects. Cancel
> is a *cooperative* action — both parties agree to void the transaction. Both cancel
> the Stripe PaymentIntent, but the status and audit trail differ.

### Step 3 — Implementation Examples

#### Node.js / Express

```javascript
const NEXTRUZT_API = 'https://api.nextruzt.io';
const API_KEY = process.env.NEXTRUZT_API_KEY;

// When client confirms an order
async function createEscrow(order) {
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
  return res.json(); // { id, status: "Funded (Held)", ... }
}

// When service is delivered and client approves
async function releaseEscrow(transactionId) {
  const res = await fetch(`${NEXTRUZT_API}/api/escrow/${transactionId}/release`, {
    method: 'POST',
    headers: {
      'X-Api-Key': API_KEY,
      'X-Idempotency-Key': `release-${transactionId}`
    }
  });
  return res.json(); // { status: "Completed (Released)", success: true }
}
```

#### Python / FastAPI

```python
import httpx, os

API_KEY = os.environ["NEXTRUZT_API_KEY"]
BASE_URL = "https://api.nextruzt.io"
HEADERS = {"X-Api-Key": API_KEY}

async def create_escrow(order: dict) -> dict:
    async with httpx.AsyncClient() as client:
        r = await client.post(f"{BASE_URL}/api/escrow/hold", json={
            "clientEmail": order["client_email"],
            "consultantEmail": order["vendor_email"],
            "amount": order["total"],
            "serviceDescription": order["description"],
            "paymentMethodId": order["stripe_pm_id"],
            "providerName": "Stripe",
        }, headers={**HEADERS, "X-Idempotency-Key": f"order-{order['id']}"})
        r.raise_for_status()
        return r.json()

async def release_escrow(transaction_id: int) -> dict:
    async with httpx.AsyncClient() as client:
        r = await client.post(
            f"{BASE_URL}/api/escrow/{transaction_id}/release",
            headers={**HEADERS, "X-Idempotency-Key": f"release-{transaction_id}"}
        )
        r.raise_for_status()
        return r.json()
```

#### C# / HttpClient

```csharp
public sealed class NexTruztClient(HttpClient http)
{
    public async Task<EscrowResponse> HoldFundsAsync(CreateEscrowRequest req)
    {
        var response = await http.PostAsJsonAsync("/api/escrow/hold", req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EscrowResponse>())!;
    }

    public async Task<EscrowResponse> ReleaseFundsAsync(int transactionId)
    {
        var response = await http.PostAsync($"/api/escrow/{transactionId}/release", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EscrowResponse>())!;
    }
}

// Registration in DI:
builder.Services.AddHttpClient<NexTruztClient>(client =>
{
    client.BaseAddress = new Uri("https://api.nextruzt.io");
    client.DefaultRequestHeaders.Add("X-Api-Key", config["NexTruzt:ApiKey"]);
});
```

### Step 4 — Handling Errors

Always check HTTP status codes. Wrap calls in retry logic for transient failures:

| Status | Action |
|--------|--------|
| `201`  | Success — store the `id` in your database for future release/dispute |
| `401`  | Check your API key — it may be expired or revoked |
| `422`  | Business rule error — read `detail` field for specifics |
| `500`  | Transient error — retry with the same `X-Idempotency-Key` |

### Step 5 — Idempotency

All mutation endpoints accept `X-Idempotency-Key` header. Use a deterministic key
(e.g., your order ID) so retries don't create duplicate transactions:

```
X-Idempotency-Key: order-12345
```

### Integration Checklist

- [ ] Obtained production API key from NexTruzt admin
- [ ] Store `transactionId` from hold response in your database
- [ ] Implement release flow when service is confirmed
- [ ] Implement dispute flow for unsatisfied clients
- [ ] Add idempotency keys to all mutation calls
- [ ] Handle error responses (401, 422, 500) with appropriate UX
- [ ] Set up monitoring/alerting on escrow status changes
- [ ] Test full lifecycle in sandbox environment
