using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class ContractKind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Kind",
                table: "Accounts",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AirDrop",
                table: "Accounts",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AirDrop",
                table: "Accounts");
        }
    }
}
