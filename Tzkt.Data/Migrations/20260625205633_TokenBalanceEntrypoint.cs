using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class TokenBalanceEntrypoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances");

            migrationBuilder.AddColumn<byte[]>(
                name: "FromEntrypoint",
                table: "TokenTransfers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ToEntrypoint",
                table: "TokenTransfers",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "OwnerEntrypoint",
                table: "Tokens",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Entrypoint",
                table: "TokenBalances",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "TokenId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances");

            migrationBuilder.DropColumn(
                name: "FromEntrypoint",
                table: "TokenTransfers");

            migrationBuilder.DropColumn(
                name: "ToEntrypoint",
                table: "TokenTransfers");

            migrationBuilder.DropColumn(
                name: "OwnerEntrypoint",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "Entrypoint",
                table: "TokenBalances");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "TokenId" },
                unique: true);
        }
    }
}
