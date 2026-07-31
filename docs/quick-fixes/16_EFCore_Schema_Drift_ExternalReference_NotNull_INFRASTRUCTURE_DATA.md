# QF-016 — `DbUpdateException`: NOT NULL Constraint on `ExternalReference` After Rename Migration

**Date:** 2026-05-02
**Layer / Concern:** Infrastructure — EF Core migrations / PostgreSQL schema
**Severity:** 🔴 First successful POST to `/api/escrow/hold` crashes on `SaveChangesAsync` — no transaction can be persisted

---

## Symptom

Auth, antiforgery, and config were all fixed. `POST /api/escrow/hold` reached `CreateAndHoldFundsHandler` and called `_repository.AddAsync(transaction, ct)` — which threw:

```
Microsoft.EntityFrameworkCore.DbUpdateException:
  An error occurred while saving the entity changes. See the inner exception for details.

  Inner Exception 1:
  PostgresException: 23502: null value in column "ExternalReference" of relation
  "Transactions" violates not-null constraint
```

The handler creates the entity **before** calling Stripe, so `ExternalReference` (which holds the Stripe `PaymentIntent.Id` once the auth succeeds) is `null` at insert time. That's intentional and correct — the column is supposed to be nullable.

---

## Root Cause — Silent Migration Drift

Two migrations evolved the column over time:

1. **`20260404012529_InitialCreate.cs:26`** — created the column as `StripePaymentIntentId` with `nullable: false`.
2. **`20260404193912_HybridIdentityAndAgnosticPersistence.cs:16-19`** — renamed `StripePaymentIntentId` → `ExternalReference` using `migrationBuilder.RenameColumn(...)`.

`RenameColumn` only emits SQL `ALTER TABLE ... RENAME COLUMN ...`. It **does not** reset constraints, defaults, or nullability. Meanwhile the model snapshot (`EscrowDbContextModelSnapshot.cs`) was regenerated and showed `ExternalReference` as nullable, matching the entity definition. Result: snapshot says nullable, database says NOT NULL — **silent drift**.

Running `dotnet ef migrations add MakeExternalReferenceNullable` produced an **empty migration** because the snapshot already matched the model. EF saw no work to do.

---

## Fix

Hand-write an `AlterColumn` migration to drop the NOT NULL constraint at the database level:

```csharp
// EscrowApp/Migrations/20260502002611_MakeExternalReferenceNullable.cs
public partial class MakeExternalReferenceNullable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ExternalReference",
            table: "Transactions",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ExternalReference",
            table: "Transactions",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
```

Apply:

```powershell
cd EscrowApp
dotnet ef database update
```

Generated SQL:

```sql
ALTER TABLE "Transactions" ALTER COLUMN "ExternalReference" DROP NOT NULL;
```

---

## Verification

```sql
-- psql
\d "Transactions"
```

Expected output for the column:

```
ExternalReference  | text |  |
```

(No `not null` qualifier.)

End-to-end:

```powershell
# Stripe key must also be set — see QF-017
curl -X POST https://localhost:7037/api/escrow/hold ...
```

Expected: `200 OK` and a row in `Transactions` with `Status = 'Held'` and `ExternalReference = 'pi_...'`.

---

## Lessons

- **`RenameColumn` is not nullability-aware.** Any rename that changes the conceptual purpose of a column should be paired with an explicit `AlterColumn` if nullability/constraints differ.
- **A model snapshot that matches the model is not proof the DB matches.** Always cross-check with `\d <table>` (Postgres) or `sp_help` (SQL Server) when investigating constraint violations.
- **`dotnet ef migrations add` returning an empty file is a smell** — investigate snapshot vs. live DB drift before assuming there is no work to do.

---

## See also

- [QF-007 EF Core migrations not applied](7_EFCore_Migrations_NotApplied_AspNetUsers_Missing_INFRASTRUCTURE_DATA.md)
