using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscrowApp.Migrations
{
    /// <inheritdoc />
    public partial class MakeExternalReferenceNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema-drift repair: the original InitialCreate migration declared
            // "StripePaymentIntentId" as NOT NULL. The HybridIdentityAndAgnosticPersistence
            // migration renamed it to "ExternalReference" and updated the model snapshot to
            // mark it nullable, but never emitted an ALTER COLUMN to drop the NOT NULL
            // constraint. Result: production INSERTs of new transactions in Pending state
            // (where ExternalReference is filled only after the payment provider hold
            // succeeds) fail with: 23502 null value in column "ExternalReference"
            // violates not-null constraint.
            migrationBuilder.AlterColumn<string>(
                name: "ExternalReference",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting requires backfilling NULLs first; this is intentionally lossy.
            migrationBuilder.Sql(
                "UPDATE \"Transactions\" SET \"ExternalReference\" = '' WHERE \"ExternalReference\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalReference",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
