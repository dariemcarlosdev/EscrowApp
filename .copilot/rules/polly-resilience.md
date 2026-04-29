# Polly Resilience Patterns — Stripe API

## Policy Composition Order (outermost → innermost)

```
Bulkhead → Circuit Breaker → Retry → Timeout
```

## Retry — Exponential Backoff with Jitter

- **3 retries**, base delay 1s, exponential multiplier ×2, plus random jitter (0–1000ms)
- Retry on: `429 Too Many Requests`, `500`, `502`, `503`, `HttpRequestException`
- **Never retry** on `4xx` client errors (except 429) — they will never succeed

## Circuit Breaker

- Break after **5 consecutive failures** within a **30-second sampling window**
- **Open state:** 60 seconds — fail fast, don't queue
- **Half-open:** allow one probe request to test recovery
- Log every state transition for operational visibility

## Timeout

- **Optimistic timeout:** 15 seconds per individual Stripe API call
- **Pessimistic timeout:** 30 seconds for entire payment operation
- Always pass and honor `CancellationToken` through the call chain

## Bulkhead Isolation

- **10 concurrent executions**, queue depth of 5 for burst absorption
- Return `503 Service Unavailable` with `Retry-After` when rejected
- Separate bulkheads for payment-critical vs non-critical operations

## Idempotency Keys — Safe Retries

- **Every** payment mutation must include an `Idempotency-Key` header
- Generate deterministically: `{TransactionId}:{Operation}:{Guid}`
- Stripe honors keys for 24 hours — retries return original response

## Configuration

Use `IOptions<StripeResilienceOptions>` — never hardcode policy values:

```csharp
public sealed class StripeResilienceOptions
{
    public int RetryCount { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 15;
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 60;
    public int BulkheadMaxParallelization { get; init; } = 10;
}
```

## HttpClient Registration

Register via `IHttpClientFactory` with `.AddPolicyHandler()` — never instantiate `HttpClient` manually.
