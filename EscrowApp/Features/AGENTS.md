# Features — MediatR CQRS Handlers

- Each feature is a vertical slice: Command + Handler + Validator + Result
- Commands: `IRequest<TResult>`, immutable records, require IdempotencyKey for payments
- Handlers: sealed class, inject interfaces only (never DbContext)
- Validate all input with FluentValidation
- Publish domain events via IEventBus AFTER persistence
- Reference existing slices: HoldFunds/, ReleaseFunds/, DisputeFunds/, GetTransaction/
- Run `dotnet build EscrowApp.sln` after changes
