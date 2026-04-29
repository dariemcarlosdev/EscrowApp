# Entity Framework Core & PostgreSQL Rules

## Provider

- Use `Npgsql.EntityFrameworkCore.PostgreSQL` — configured via `UseNpgsql()` in `Program.cs`
- Connection string from `appsettings.json` under `DefaultConnection`

## Type Mappings

| C# Type | PostgreSQL Type | Notes |
|---------|----------------|-------|
| `decimal` | `numeric(18,4)` | Monetary values — never `real` or `double precision` |
| `Guid` | `uuid` | Native PostgreSQL UUID support |
| `DateTimeOffset` | `timestamptz` | All timestamps |
| Dictionary/JSON | `jsonb` | Semi-structured metadata |

## Repository Pattern

- `IEscrowTransactionRepository` defined in Application/Domain layer
- `EscrowTransactionRepository` implementation in `Data/Repositories/`
- **Never inject `EscrowDbContext` into Features/, Services/, or Components/**
- **Never expose `IQueryable<T>`** from repositories — it leaks persistence concerns
- Repository methods return domain entities — mapping to DTOs happens in handlers

## Query Rules

- Read queries: always `AsNoTracking()` for performance
- Write operations: load entity → modify → `SaveChangesAsync()` inside repository
- **Always parameterized queries** — never string-concatenate user input into SQL
- If raw SQL needed: use `FromSqlInterpolated` — never `FromSqlRaw` with concatenation

## Configuration

- Use `IEntityTypeConfiguration<T>` in separate files — loaded via `ApplyConfigurationsFromAssembly`
- Define unique constraints: `IdempotencyKey` on `EscrowTransaction`
- Define indexes: `Status`, `CreatedAt`, `StripePaymentIntentId`
- Configure relationships explicitly — never rely on convention for DDD models
- Domain entities have no EF Core attributes — all mapping via Fluent API

## Migrations

- Create: `dotnet ef migrations add MigrationName`
- Apply: `dotnet ef database update`
- Check existing migrations in `Migrations/` before creating new ones
