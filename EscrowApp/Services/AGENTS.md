# Services — Payment Strategies & Infrastructure

- Strategy interfaces (ISP-compliant): IFundHoldable, IFundReleasable, IFundCancellable
- Every payment operation MUST include an idempotencyKey parameter
- Stripe: manual capture (capture_method: manual) — authorize then capture on release
- Never modify payment amounts between authorization and capture
- Polly resilience: retry (3x exponential), circuit breaker, 15s timeout, bulkhead
- Never log PII, tokens, API keys, or connection strings
- Register via IHttpClientFactory — never instantiate HttpClient directly
