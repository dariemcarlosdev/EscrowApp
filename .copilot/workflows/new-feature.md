---
description: Create a new MediatR vertical slice feature
---

1. Create feature directory under `EscrowApp/Features/Escrow/{FeatureName}/`

2. Create command/query record
   - Commands: `{Verb}{Noun}Command.cs` implementing `IRequest<{Result}>`
   - Queries: `Get{Noun}Query.cs` implementing `IRequest<{Result}>`

3. Create handler
   - `{FeatureName}Handler.cs` implementing `IRequestHandler<,>`
   - Inject interfaces only (IEscrowTransactionRepository, IEventBus, etc.)

4. Create FluentValidation validator
   - `{FeatureName}Validator.cs` extending `AbstractValidator<>`

5. Create result DTO as a `record` type

6. Run build to verify
   dotnet build EscrowApp.sln // turbo

7. Update docs
   - Update `docs/features/` with new feature documentation
   - Update `docs/planning/task-checklist.md`
