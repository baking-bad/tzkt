using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class AccountIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Staked",
                table: "Accounts",
                column: "Staked");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type",
                table: "Accounts",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type_Kind",
                table: "Accounts",
                columns: new[] { "Type", "Kind" },
                filter: "\"Type\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type_Staked",
                table: "Accounts",
                columns: new[] { "Type", "Staked" },
                filter: "\"Type\" = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_Staked",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Type",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Type_Kind",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Type_Staked",
                table: "Accounts");
        }
    }
}
