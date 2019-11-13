using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class ProtoLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Protocols");

            migrationBuilder.AddColumn<int>(
                name: "FirstLevel",
                table: "Protocols",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastLevel",
                table: "Protocols",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FirstLevel",
                table: "Accounts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastLevel",
                table: "Accounts",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FirstLevel",
                table: "Accounts",
                column: "FirstLevel");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Blocks_FirstLevel",
                table: "Accounts",
                column: "FirstLevel",
                principalTable: "Blocks",
                principalColumn: "Level",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Blocks_FirstLevel",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_FirstLevel",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "FirstLevel",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "LastLevel",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "FirstLevel",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastLevel",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
