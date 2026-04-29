# CQRS & MediatR Patterns

## Vertical Slice Structure

Each use case is a self-contained folder under `Features/Escrow/`:

```
Features/Escrow/{FeatureName}/
├── {Name}Command.cs           ← IRequest<TResult> record
├── {Name}CommandValidator.cs  ← FluentValidation
├── {Name}Handler.cs           ← IRequestHandler<,>
└── {Name}Result.cs            ← Result DTO record
```

One command/query, one handler, one result per folder. No shared handlers.

## Command vs Query

| Aspect | Command (Write) | Query (Read) |
|--------|----------------|--------------|
| Naming | `{Verb}{Noun}Command` | `Get{Noun}Query` |
| Returns | Result DTO with success/error | DTO or collection |
| Side effects | Yes — DB writes, events, payments | None — read-only |
| Validation | Always required | Optional |
| Idempotency | Required for payment commands | N/A |
| EF Tracking | Default tracking | `AsNoTracking()` |

## Handler Orchestration Flow

```
1. Validate input (guard clauses or FluentValidation)
2. Resolve strategy via IPaymentStrategyFactory (if payment op)
3. Execute operation via strategy interface
4. Persist via IEscrowTransactionRepository
5. Publish domain event via IEventBus (after persistence)
6. Return result DTO
```

## Existing Slices

| Slice | Type | Purpose |
|-------|------|---------|
| `CreateAndHoldFunds/` | Command | Create transaction + hold funds atomically |
| `HoldFunds/` | Command | Hold funds on existing transaction |
| `ReleaseFunds/` | Command | Capture held funds |
| `DisputeFunds/` | Command | Flag transaction as disputed |
| `GetTransaction/` | Query | Read single transaction by ID |
| `ListTransactions/` | Query | List transactions with filtering |

## Rules

- Commands/queries are immutable `record` types with `init` properties
- Handlers are `sealed` classes using primary constructors for DI
- Every async method accepts and propagates `CancellationToken`
- Handlers inject interfaces only — never concrete infrastructure types
