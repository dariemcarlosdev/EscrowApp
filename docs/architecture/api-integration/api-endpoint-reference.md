# API Endpoint Reference

**Status:** Implemented (Development Swagger inventory)

> Source of truth for this document: `EscrowController.cs` and `Program.cs`.

## OpenAPI / Swagger

| Item | Local URL | Notes |
|---|---|---|
| Swagger UI | `https://localhost:7037/swagger` | Enabled in Development only |
| Swagger UI (HTTP) | `http://localhost:5093/swagger` | Local HTTP profile |
| OpenAPI JSON | `https://localhost:7037/swagger/v1/swagger.json` | Generated from the running app |
| OpenAPI JSON (HTTP) | `http://localhost:5093/swagger/v1/swagger.json` | Local HTTP profile |

## Authentication Matrix

| Area | Auth model | Notes |
|---|---|---|
| `/api/escrow/*` | `X-Api-Key` header + `ApiAccess` policy | Third-party integration surface |
| `/auth/logout` | Authenticated app session | Cookie-backed Blazor auth flow |
| `/culture/set` | Anonymous | Sets culture cookie, then redirects |
| `POST /api/webhooks/stripe` | Stripe signature verification | Uses `Stripe-Signature`, not app auth |
| `GET /api/webhooks/stripe` | Anonymous, Development only | Diagnostic route for manual checks |

## Endpoint Group Locations in the Project

| Endpoint group | Project location | Notes |
|---|---|---|
| Escrow API endpoints | `EscrowApp\Features\Escrow\Api\EscrowController.cs` | Controller-based REST surface under `/api/escrow` |
| Escrow API request/response contracts | `EscrowApp\Features\Escrow\Api\ApiContracts.cs` | DTOs used by the controller endpoints |
| Minimal app endpoints | `EscrowApp\Program.cs` | Defines `/culture/set` and `/auth/logout` |
| Stripe webhook route mapping | `EscrowApp\Program.cs` | Maps the Development `GET` diagnostic and `POST` webhook callback |
| Stripe webhook handlers | `EscrowApp\Infrastructure\Webhooks\Stripe\StripeWebhookEndpoint.cs` | Implements `HandleStatus()` and `HandleAsync()` |

## Endpoint Inventory

### EscrowController
inserts the full set of API endpoints related to escrow transactions, including creation, retrieval, release, dispute, and cancellation operations. Each endpoint is designed to handle specific aspects of the escrow lifecycle, with appropriate authentication and request/response models.
**Defined in:** `EscrowApp\Features\Escrow\Api\EscrowController.cs`

| Method | Path | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| POST | `/api/escrow/hold` | `X-Api-Key` | `CreateAndHoldRequest` body, optional `X-Idempotency-Key` header | `201 Created` + `EscrowTransactionResponse` | Atomic create-and-hold entry point |
| GET | `/api/escrow/{id}` | `X-Api-Key` | Route `id:int` | `200 OK` + `EscrowTransactionResponse` | Returns `404` when not found |
| GET | `/api/escrow` | `X-Api-Key` | Query: `page`, `pageSize`, optional `status` | `200 OK` + `PaginatedResponse<EscrowTransactionResponse>` | Paged list endpoint |
| POST | `/api/escrow/{id}/release` | `X-Api-Key` | Route `id:int`, optional `X-Idempotency-Key` header | `200 OK` + `ReleaseFundsResult` | Releases held funds |
| POST | `/api/escrow/{id}/dispute` | `X-Api-Key` | `DisputeFundsApiRequest` body, optional `X-Idempotency-Key` header | `200 OK` + `DisputeFundsResult` | `RaisedBy` comes from authenticated identity |
| POST | `/api/escrow/{id}/cancel` | `X-Api-Key` | `CancelFundsApiRequest` body, optional `X-Idempotency-Key` header | `200 OK` + `CancelFundsResult` | Cooperative cancellation path |

### Application Minimal Endpoints

**Defined in:** `EscrowApp\Program.cs`

| Method | Path | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | `/culture/set` | Anonymous | Query: `culture`, `redirectUri` | Redirect | Accepts `en`, `es`, `en-US`, `es-MX`; sets culture cookie |
| POST | `/auth/logout` | Authenticated session | No body required | Redirect to `/` | Deletes auth cookie; antiforgery disabled on this endpoint |

### Stripe Webhook Endpoints

**Defined in:** route mapping in `EscrowApp\Program.cs`; handler implementation in `EscrowApp\Infrastructure\Webhooks\Stripe\StripeWebhookEndpoint.cs`

| Method | Path | Auth | Request | Response | Notes |
|---|---|---|---|---|---|
| GET | `/api/webhooks/stripe` | Anonymous | None | `200 OK` + `StripeWebhookStatusResponse` | Development-only diagnostic route |
| POST | `/api/webhooks/stripe` | Stripe signature verification | Raw body + `Stripe-Signature` header | `204 No Content` on success | Processes `payment_intent.succeeded`; unsupported event types are logged and ignored |

## Request Models

### CreateAndHoldRequest

| Field | Type | Required | Notes |
|---|---|---|---|
| `clientEmail` | `string` | Yes | Email address |
| `consultantEmail` | `string` | Yes | Email address |
| `amount` | `decimal` | Yes | Range `0.01` to `1_000_000` |
| `serviceDescription` | `string` | Yes | Max length `500` |
| `paymentMethodId` | `string` | Yes | Stripe payment method identifier |
| `providerName` | `string` | No | Defaults to `"Stripe"`; max length `50` |

### DisputeFundsApiRequest

| Field | Type | Required | Notes |
|---|---|---|---|
| `reason` | `string` | Yes | Max length `1000` |

### CancelFundsApiRequest

| Field | Type | Required | Notes |
|---|---|---|---|
| `reason` | `string` | Yes | Max length `1000` |

## Response Models

### EscrowTransactionResponse

| Field | Type | Notes |
|---|---|---|
| `id` | `int` | Transaction identifier |
| `clientEmail` | `string` | Payer email |
| `consultantEmail` | `string` | Payee email |
| `amount` | `decimal` | Base transaction amount |
| `serviceDescription` | `string` | Human-readable description |
| `status` | `string` | Current transaction state |
| `externalReference` | `string?` | External provider reference, such as Stripe PaymentIntent ID |
| `externalProvider` | `string?` | Provider name, such as Stripe |
| `disputeReason` | `string?` | Present when disputed |
| `platformFee` | `decimal` | Fee amount charged by the platform |
| `platformFeePercentage` | `decimal` | Fee rate used to calculate the fee |
| `totalCharged` | `decimal` | Computed as `amount + platformFee` |
| `createdAt` | `DateTime` | UTC timestamp |

### PaginatedResponse<T>

| Field | Type | Notes |
|---|---|---|
| `items` | `IReadOnlyList<T>` | Current page items |
| `page` | `int` | Current page number |
| `pageSize` | `int` | Requested page size |
| `totalCount` | `int` | Total items across all pages |
| `totalPages` | `int` | Computed page count |

### ReleaseFundsResult

| Field | Type |
|---|---|
| `transactionId` | `int` |
| `status` | `string` |
| `success` | `bool` |

### DisputeFundsResult

| Field | Type |
|---|---|
| `transactionId` | `int` |
| `status` | `string` |
| `holdCancelled` | `bool` |
| `disputeReason` | `string` |

### CancelFundsResult

| Field | Type |
|---|---|
| `transactionId` | `int` |
| `status` | `string` |
| `externalReference` | `string` |
| `externalProvider` | `string` |
| `reason` | `string` |
| `cancelledBy` | `string` |

### StripeWebhookStatusResponse

| Field | Type | Notes |
|---|---|---|
| `endpoint` | `string` | Always `/api/webhooks/stripe` |
| `acceptedMethod` | `string` | `POST` |
| `status` | `string` | Current readiness indicator |
| `message` | `string` | Explains that Stripe deliveries must use `POST` |

## Default Response Behavior

| Endpoint type | Common status codes |
|---|---|
| Escrow API reads/writes | `200`, `201`, `400`, `401`, `403`, `404`, `422`, `500` |
| Culture switch | `302` redirect or `400` for unsupported culture |
| Logout | `302` redirect to `/` |
| Webhook diagnostic GET | `200` |
| Webhook POST | `204`, `400`, `401`, `500` |

## Notes

1. Swagger is configured globally with an API key security definition, but the minimal endpoints do not all use the same auth flow as `/api/escrow/*`.
2. `POST /api/webhooks/stripe` is operationally separate from the partner API surface and should be treated as inbound infrastructure, not a public client integration endpoint.
3. `GET /api/webhooks/stripe` exists only to make local manual checks clearer during Development.

⚠️ Compliance-sensitive — requires legal review before production if adapted into partner-facing or user-facing documentation.
