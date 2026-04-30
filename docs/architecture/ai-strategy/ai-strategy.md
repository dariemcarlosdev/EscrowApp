# AI Strategy — Architecture Decision Record

> NexTruzt.io AI integration architecture.
> Status: **Planned** (not implemented)
> Last synced with codebase: 2026-04-10
> Cross-references: [AI Features Roadmap](../../modules/system/ai-features/ai-features-roadmap.md) · [Architecture Overview](../overview/architecture-overview.md) · [Payment Strategies](../payment-strategies/payment-strategies.md)

---

## Decision Context

NexTruzt.io requires AI features to differentiate from Escrow.com, Upwork, and Stripe DIY. This ADR documents **how** AI integrates with the existing Clean Architecture + CQRS stack — not **what** AI features to build (see [AI Features Roadmap](../../modules/system/ai-features/ai-features-roadmap.md)).

## Decision

AI capabilities follow the same architectural patterns as payment strategies: **domain defines contracts, infrastructure fulfills them**.

---

## Layer Placement

```
Components/                          Blazor UI — AI-assisted components
    │  EventCallback<T> / IMediator
    ▼
Features/Ai/{FeatureName}/          MediatR handlers — orchestrate AI calls
    │  IAiTextGenerationService
    ▼
Models/                              Domain — NO AI dependencies
    ▲
    │  implements interfaces
Services/Ai/                         Infrastructure — LLM provider implementations
```

### Rules

| Layer | AI Responsibility | Must NOT |
|-------|-------------------|----------|
| Domain (Models/Events) | Zero AI awareness | Reference any AI SDK or service |
| Application (Features/Ai/) | Define `IAiTextGenerationService` interface; orchestrate via MediatR handlers | Call LLM APIs directly |
| Infrastructure (Services/Ai/) | Implement `IAiTextGenerationService`; manage HTTP calls, auth, retry | Contain business logic |
| Presentation (Components/) | Render AI results; handle loading/error states | Call AI services directly (use `IMediator`) |

---

## Strategy Pattern Extension

AI follows the same ISP pattern as payment providers:

```
Existing Payment:                    New AI:
IEscrowPaymentStrategy (marker)      IAiServiceStrategy (marker)
├── IFundHoldable                    ├── IAiTextGenerationService
├── IFundReleasable                  ├── IAiClassificationService (Phase 2)
└── IFundCancellable                 └── IAiEmbeddingService (Phase 3)
```

### MVP Interface (Phase 1 — single capability)

```csharp
// Application layer — Features/Ai/IAiTextGenerationService.cs
public interface IAiTextGenerationService
{
    Task<AiGenerationResult> GenerateAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AiGenerationRequest(
    string SystemPrompt,
    string UserPrompt,
    string Locale,
    int MaxTokens = 500);

public sealed record AiGenerationResult(
    string GeneratedText,
    bool IsSuccess,
    string? ErrorMessage = null);
```

### Factory (defer until 2+ providers)

```csharp
// NOT for MVP — build when adding a second AI provider
public interface IAiStrategyFactory
{
    IAiTextGenerationService Resolve(string providerName);
}
```

**MVP:** Register single implementation directly in DI. No factory needed.

---

## Infrastructure: Minimal MVP Setup

### Configuration (Options Pattern)

```csharp
public sealed class AiServiceSettings
{
    public const string SectionName = "Ai";
    public bool Enabled { get; init; } = false;
    public string Endpoint { get; init; } = string.Empty;
    public string DeploymentName { get; init; } = string.Empty;
    // API key via environment variable — NEVER in appsettings.json
}
```

### DI Registration (Program.cs)

```csharp
builder.Services.Configure<AiServiceSettings>(
    builder.Configuration.GetSection(AiServiceSettings.SectionName));
builder.Services.AddScoped<IAiTextGenerationService, AzureOpenAiTextGenerationService>();
```

### Resilience (MVP — simple)

```
MVP:     try/catch → graceful fallback (manual input)
Phase 2: Polly retry (429/5xx) + circuit breaker (add when AI call volume justifies it)
```

Do NOT build Polly infrastructure for AI until transaction volume exceeds 100/day.

---

## Security Constraints

| Constraint | Enforcement |
|------------|-------------|
| **No PII in prompts** | Send only service description text — never emails, amounts, names, or payment IDs |
| **No secrets in code** | API key via `AI__ApiKey` environment variable |
| **Prompt injection defense** | User text wrapped as data parameter, never interpolated into system instructions |
| **Rate limiting** | Per-user/session limit enforced in MediatR handler |
| **Output validation** | Verify AI response is text, not code/scripts; sanitize before rendering |
| **Audit trail** | Log AI usage (correlation ID + feature name) — never log prompts or responses containing user text |

---

## Cost Model

| Scale | Calls/month | Tokens/call (est.) | Monthly cost (GPT-4o-mini) |
|-------|-------------|--------------------|-----------------------------|
| Early (10 users) | ~50 | ~800 | < $0.50 |
| Growth (100 users) | ~500 | ~800 | < $5.00 |
| Scale (1000 users) | ~5,000 | ~800 | < $50.00 |

**Budget gate:** If AI costs exceed 1% of platform fee revenue, review pricing or model selection.

---

## Upgrade Triggers

| Milestone | Action |
|-----------|--------|
| First AI feature ships | Build `IAiTextGenerationService` + single implementation |
| Second AI feature ships | Extract common middleware (rate limiting, logging) |
| 2+ AI providers needed | Build `IAiStrategyFactory` (mirrors `IPaymentStrategyFactory`) |
| AI call volume > 100/day | Add Polly resilience policies |
| Classification needed | Add `IAiClassificationService` interface |
| RAG/embeddings needed | Add `IAiEmbeddingService` + vector store |

---

## Anti-Patterns (do NOT build)

| Anti-Pattern | Why Not |
|---|---|
| Generic `IAiService` god interface | Violates ISP — use focused capability interfaces |
| Feature flag framework for AI | One `Ai:Enabled` bool in config is sufficient for MVP |
| PII redaction middleware | Don't send PII in the first place — design prompts to exclude it |
| AI-as-source-of-truth | AI assists, never decides — deterministic logic owns state transitions |
| Local/self-hosted models | Hosted APIs (Azure OpenAI) are cheaper to operate at MVP scale |
