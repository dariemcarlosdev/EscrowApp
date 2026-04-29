# 20 — Input Validation

> FluentValidation pipeline behavior for all MediatR commands — Track A, Item #4.

## Overview

All MediatR commands must be validated before handler execution. Validation is implemented as a **MediatR pipeline behavior** (`ValidationBehavior<TRequest, TResponse>`) using **FluentValidation**, ensuring:

- Consistent, testable validation rules per command
- Automatic 400 Bad Request responses on invalid input (via `ApiExceptionMiddleware`)
- Idempotency key enforcement across all payment operations
- No handler executes with invalid data — fail fast at the boundary

---

## Architecture: Pipeline Behavior

```
HTTP Request → EscrowController → IMediator.Send(command)
                                        ↓
                              ValidationBehavior<TRequest, TResponse>
                                  ↓ (if valid)
                              Handler (HoldFundsHandler, etc.)
                                  ↓ (if invalid)
                              throws ValidationException → ApiExceptionMiddleware → 400
```

The behavior is registered **once** in `Program.cs` and applies to every `IRequest<T>` automatically — no per-handler wiring required.

---

## Pipeline Behavior Implementation

```csharp
// Features/Shared/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context  = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### DI Registration (`Program.cs`)

```csharp
// Register FluentValidation — discovers validators from assembly
services.AddValidatorsFromAssemblyContaining<Program>();

// Register pipeline behavior
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
```

> **Note:** `ApiExceptionMiddleware` must handle `ValidationException` and return RFC 7807 ProblemDetails with status 400. Confirm this is already wired in `Infrastructure/Middleware/ApiExceptionMiddleware.cs`.

---

## Command Validators

### 1. `CreateAndHoldFundsCommandValidator`

```csharp
// Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsCommandValidator.cs
public sealed class CreateAndHoldFundsCommandValidator
    : AbstractValidator<CreateAndHoldFundsCommand>
{
    public CreateAndHoldFundsCommandValidator()
    {
        RuleFor(x => x.EscrowAmount)
            .GreaterThan(0).WithMessage("Escrow amount must be greater than zero.")
            .LessThanOrEqualTo(500_000).WithMessage("Single transaction limit is $500,000.");

        RuleFor(x => x.ClientEmail)
            .NotEmpty().WithMessage("Client email is required.")
            .EmailAddress().WithMessage("Client email must be a valid email address.");

        RuleFor(x => x.ConsultantEmail)
            .NotEmpty().WithMessage("Consultant email is required.")
            .EmailAddress().WithMessage("Consultant email must be a valid email address.")
            .NotEqual(x => x.ClientEmail).WithMessage("Client and consultant cannot be the same person.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255).WithMessage("Idempotency key cannot exceed 255 characters.");
    }
}
```

**Business rules enforced:**
- Amount > 0 (no zero-value payments)
- Amount ≤ $500K (fraud / Stripe limit protection — configurable post-MVP)
- Valid email format for both parties (ensures notification routing works)
- Client ≠ Consultant (prevents self-dealing)
- Description required (audit trail completeness)
- Idempotency key required (fintech guardrail — see `AGENTS.md`)

---

### 2. `HoldFundsCommandValidator`

```csharp
// Features/Escrow/HoldFunds/HoldFundsCommandValidator.cs
public sealed class HoldFundsCommandValidator
    : AbstractValidator<HoldFundsCommand>
{
    public HoldFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255);
    }
}
```

---

### 3. `ReleaseFundsCommandValidator`

```csharp
// Features/Escrow/ReleaseFunds/ReleaseFundsCommandValidator.cs
public sealed class ReleaseFundsCommandValidator
    : AbstractValidator<ReleaseFundsCommand>
{
    public ReleaseFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255).WithMessage("Idempotency key cannot exceed 255 characters.");
    }
}
```

---

### 4. `DisputeFundsCommandValidator`

```csharp
// Features/Escrow/DisputeFunds/DisputeFundsCommandValidator.cs
public sealed class DisputeFundsCommandValidator
    : AbstractValidator<DisputeFundsCommand>
{
    public DisputeFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Dispute reason is required.")
            .MinimumLength(10).WithMessage("Dispute reason must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Dispute reason cannot exceed 1,000 characters.");

        RuleFor(x => x.DisputedBy)
            .NotEmpty().WithMessage("DisputedBy (email) is required.")
            .EmailAddress().WithMessage("DisputedBy must be a valid email address.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255);
    }
}
```

**Business rule enforced:** Reason minimum 10 characters prevents trivial `"no"` disputes — supports audit trail quality.

---

### 5. `CancelFundsCommandValidator`

```csharp
// Features/Escrow/CancelFunds/CancelFundsCommandValidator.cs
public sealed class CancelFundsCommandValidator
    : AbstractValidator<CancelFundsCommand>
{
    public CancelFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MinimumLength(5).WithMessage("Cancellation reason must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");

        RuleFor(x => x.CancelledBy)
            .NotEmpty().WithMessage("CancelledBy (email) is required.")
            .EmailAddress().WithMessage("CancelledBy must be a valid email address.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255);
    }
}
```

---

## Error Response Format

`ApiExceptionMiddleware` must map `ValidationException` → RFC 7807 `ProblemDetails`:

```jsonc
// 400 Bad Request
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "EscrowAmount": ["Escrow amount must be greater than zero."],
    "IdempotencyKey": ["Idempotency key is required."]
  }
}
```

---

## Files

| File | Action | Purpose |
|---|---|---|
| `Features/Shared/ValidationBehavior.cs` | **Create** | MediatR pipeline behavior |
| `Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsCommandValidator.cs` | **Create** | Validator |
| `Features/Escrow/HoldFunds/HoldFundsCommandValidator.cs` | **Create** | Validator |
| `Features/Escrow/ReleaseFunds/ReleaseFundsCommandValidator.cs` | **Create** | Validator |
| `Features/Escrow/DisputeFunds/DisputeFundsCommandValidator.cs` | **Create** | Validator |
| `Features/Escrow/CancelFunds/CancelFundsCommandValidator.cs` | **Create** | Validator |
| `Program.cs` | **Modify** | Register `AddValidatorsFromAssemblyContaining<Program>()` + pipeline behavior |
| `Infrastructure/Middleware/ApiExceptionMiddleware.cs` | **Modify** | Handle `ValidationException` → 400 ProblemDetails |

---

## NuGet Packages Required

```xml
<!-- EscrowApp.csproj -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
```

> **Note:** `FluentValidation.AspNetCore` is the preferred package for ASP.NET Core / .NET 10. Do not use the deprecated `FluentValidation.DependencyInjectionExtensions` standalone package.

---

## Business Rules Summary

| Rule | Validator | Rationale |
|---|---|---|
| Amount > 0 | CreateAndHold | No zero-value payment authorizations |
| Amount ≤ $500K | CreateAndHold | Stripe auth limit protection; fraud surface reduction |
| Client ≠ Consultant | CreateAndHold | Prevents self-dealing |
| Emails must be valid format | CreateAndHold, Release, Dispute, Cancel | Notification routing; PII validation |
| Idempotency key required on all payment ops | All | Fintech guardrail — prevents duplicate charges on retry |
| Dispute reason ≥ 10 chars | Dispute | Audit trail quality; prevents trivial disputes |
| Transaction ID > 0 | Hold, Release, Dispute, Cancel | Database ID sanity check |

---

## Testing Notes

Each validator should have at minimum 2–3 unit tests (valid case + each invalid path):

```
CreateAndHoldFundsCommandValidatorTests
  ├── Should_Pass_When_All_Fields_Valid
  ├── Should_Fail_When_Amount_Is_Zero
  ├── Should_Fail_When_Emails_Are_Equal
  └── Should_Fail_When_IdempotencyKey_Is_Empty
```

See `docs/cross-cutting/testing/` for the full test strategy (xUnit + FluentAssertions + Moq).
