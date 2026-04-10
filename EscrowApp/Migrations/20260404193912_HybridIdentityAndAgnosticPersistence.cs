using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EscrowApp.Migrations
{
    /// <inheritdoc />
    public partial class HybridIdentityAndAgnosticPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename preserves existing Stripe PaymentIntent IDs (data-safe)
            migrationBuilder.RenameColumn(
                name: "StripePaymentIntentId",
                table: "Transactions",
                newName: "ExternalReference");

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    WalletAddress = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentityMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityMappings_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityMappings_ActorId",
                table: "IdentityMappings",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityMappings_Provider_ExternalId",
                table: "IdentityMappings",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityMappings");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "ExternalReference",
                table: "Transactions",
                newName: "StripePaymentIntentId");
        }
    }
}
