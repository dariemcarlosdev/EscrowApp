# Testing Standards

## Framework & Tooling

- **Test framework:** xUnit — `[Fact]` for single cases, `[Theory]` with `[InlineData]` for parameterized
- **Assertions:** FluentAssertions — `.Should().Be()`, `.Should().Throw<T>()`
- **Mocking:** Moq — `new Mock<IRepository>()`
- **Integration:** `WebApplicationFactory<Program>` for API tests
- **Database:** Testcontainers for PostgreSQL integration tests

## Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Clear intent
HoldFunds_ValidTransaction_ReturnsSuccess()
HoldFunds_InsufficientBalance_ThrowsPaymentException()
RaiseDispute_TransactionNotHeld_ThrowsInvalidStateException()

// ❌ Vague
Test1()
TestHoldFunds()
```

## Arrange-Act-Assert (AAA)

Every test has clearly separated AAA sections with blank lines between them.

## What to Test

| Layer | What to Test |
|-------|-------------|
| Domain Models | Constructor validation, state transitions, domain events, value object equality |
| MediatR Handlers | Orchestration logic, correct repo/strategy calls, error handling |
| Strategy Impls | Mocked Stripe SDK, correct PaymentIntent parameters |
| Validators | Required fields, boundary values, format constraints |
| API Endpoints | Full HTTP cycle, status codes, response bodies |

## What NOT to Test

- Private methods — test through public interface
- Framework behavior — ASP.NET routing, DI resolution
- Third-party internals — mock the boundary

## Coverage Targets

- **Critical payment flows** (hold, release, cancel, dispute): **>90% line coverage**
- **Domain model invariants**: **100%** — every state transition tested
- **API endpoints**: every documented status code has at least one test

## Test Data — Builder Pattern

Use builders for domain objects to keep tests readable and decoupled from constructor changes.
