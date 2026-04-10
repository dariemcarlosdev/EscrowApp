# Data — Infrastructure Layer (EF Core + PostgreSQL)

- EscrowDbContext is scoped; never expose outside this layer
- Repository pattern: IEscrowTransactionRepository interface → concrete implementation
- AsNoTracking() for all read queries
- Parameterized queries ONLY — never concatenate user input into SQL
- decimal → numeric(18,4) for monetary values
- Fluent API configurations in separate IEntityTypeConfiguration<T> files
- Migrations: `dotnet ef migrations add <Name>` — review before applying
