# 16 — Testing Strategy

> Test architecture, conventions, and coverage targets for the EscrowApp.

## Overview

The EscrowApp test suite lives in `EscrowApp.Tests/` as a separate project in the solution. It uses **xUnit** as the test framework, **FluentAssertions** for readable assertions, and **Moq** for mocking dependencies.

## Test Project Structure

```
EscrowApp.Tests/
├── EscrowApp.Tests.csproj         ← Project file (xUnit + FluentAssertions + Moq)
├── GlobalUsings.cs                ← Common using statements
├── Features/
│   └── Escrow/
│       ├── HoldFundsHandlerTests.cs
│       ├── ReleaseFundsHandlerTests.cs
│       ├── DisputeFundsHandlerTests.cs
│       └── CancelFundsHandlerTests.cs
└── Services/
    └── Strategies/
        └── StripePaymentStrategyTests.cs
```

## Technology Stack

| Tool | Version | Purpose |
|------|---------|---------|
| xUnit | 2.9.x | Test framework |
| FluentAssertions | 8.x | Readable assertion syntax |
| Moq | 4.20.x | Mock framework for interfaces |
| coverlet | 6.x | Code coverage collection |
| Microsoft.NET.Test.Sdk | 17.x | Test host |

## Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `Handle_ValidPendingTransaction_HoldsFundsAndReturnsSuccess`
- `Handle_TransactionNotFound_ThrowsException`
- `Handle_DisputedTransaction_ThrowsInvalidOperationException`

## Test Categories

### 1. MediatR Handler Tests (Unit)

Test each command/query handler in isolation:
- Mock `IEscrowTransactionRepository`, `IPaymentStrategyFactory`, `IEventBus`
- Verify correct state transitions
- Verify domain events are published after successful operations
- Verify exceptions on invalid state transitions

**Coverage target:** Every handler's happy path + every invalid state transition.

### 2. FluentValidation Validator Tests (Unit)

Test each command validator in isolation using **FluentValidation.TestHelper**:

```csharp
[Fact]
public async Task Validate_ValidInput_Passes()
{
    var command = new CreateAndHoldFundsCommand(
        clientEmail: "client@ex.com",
        consultantEmail: "consultant@ex.com",
        amount: 100m,
        serviceDescription: "Services",
        paymentMethodId: "pm_visa",
        idempotencyKey: "key-123");

    var result = await validator.TestValidateAsync(command);
    
    result.ShouldNotHaveAnyValidationErrors();
}

[Fact]
public async Task Validate_AmountZero_Fails()
{
    var command = new CreateAndHoldFundsCommand(
        // ... amount: 0m ...
    );

    var result = await validator.TestValidateAsync(command);
    
    result
        .ShouldHaveValidationErrorFor(x => x.Amount)
        .WithErrorMessage("*greater than zero*");
}
```

**Pattern:** 
- One test per validation rule (positive + negative paths)
- Use `TestValidate()` / `TestValidateAsync()` from FluentValidation.TestHelper
- Verify both rule name and error message

**Coverage target:** Every validation rule in every validator (CreateAndHold: 9 rules, HoldFunds: 3, ReleaseFunds: 2, DisputeFunds: 4, CancelFunds: 4 = 22+ test cases).

**Key files:**
- `Features/Behaviors/ValidationBehavior.cs` — Pipeline behavior that runs all validators
- `Features/Escrow/*/[Command]Validator.cs` — 5 validators with business rules
- `EscrowApp.Tests/Features/Escrow/*ValidatorTests.cs` — Test classes

### 3. Strategy Tests (Unit)

Test `StripePaymentStrategy` with mocked Stripe SDK:
- Verify PaymentIntent creation uses `capture_method: manual`
- Verify idempotency keys are passed to Stripe
- Verify correct Stripe API calls for hold/release/cancel
- Verify Stripe exception mapping to domain exceptions

### 4. Domain Model Tests (Unit — Future)

Test `EscrowTransaction` entity behavior:
- State transition validation (valid/invalid paths)
- Constructor validation (required fields, amount > 0)
- Domain event emission on state changes

### 5. API Integration Tests (Future)

Test full HTTP request/response cycle:
- Use `WebApplicationFactory<Program>`
- Test endpoint routing, auth, validation, and response codes
- Use Testcontainers for PostgreSQL (real database per test class)

### 6. Webhook Tests (Future)

Test webhook processing:
- Signature verification with valid/invalid signatures
- Event type routing to correct handler
- State updates from webhook events

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~CancelFundsHandlerTests"

# Run in CI
dotnet test --no-build -c Release --verbosity normal
```

## Coverage Targets

| Area | Target | Rationale |
|------|--------|-----------|
| Payment handlers (Hold, Release, Cancel) | > 90% | Financial operations — high risk |
| Domain model state transitions | 100% | Business invariants must be bulletproof |
| API endpoints | Every documented status code | Contract compliance |
| Webhook processing | > 80% | External integration — failure-prone |
| UI components | Not tested (MVP) | Blazor component testing deferred |

## Current Status

All test files contain **skeleton structure with TODO placeholders**. Tests use `Assert.True(true, "Placeholder")` to pass the build while implementations are pending. This is tracked as **MVP Task #5** in the [task checklist](../../../planning/task-checklist.md).

**Test count:** 16 placeholder tests across 5 test files — all pass but test nothing real.

**Implementation priority:**
1. HoldFundsHandlerTests — 3 real tests (core payment flow)
2. ReleaseFundsHandlerTests — 3 real tests (revenue-critical)
3. DisputeFundsHandlerTests — 2 real tests (risk management)
4. CancelFundsHandlerTests — 4 real tests (depends on CancelFunds handler implementation)
5. StripePaymentStrategyTests — 4 real tests with mocked Stripe SDK (integration boundary)

## CI Integration

Tests run automatically via GitHub Actions (`.github/workflows/ci.yml`):
- Every push to `main` or `develop`
- Every pull request targeting `main`
- Coverage reports uploaded as build artifacts
