using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class ScriptHashes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodeHash",
                table: "Scripts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TypeHash",
                table: "Scripts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodeHash",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeHash",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CodeHash",
                table: "Accounts",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TypeHash",
                table: "Accounts",
                column: "TypeHash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_CodeHash",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_TypeHash",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "TypeHash",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TypeHash",
                table: "Accounts");
        }
    }
}
