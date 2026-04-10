using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscrowApp.Migrations
{
    /// <inheritdoc />
    public partial class DisputeFundsSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "Transactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "Transactions");
        }
    }
}
