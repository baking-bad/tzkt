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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstLevel",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "LastLevel",
                table: "Protocols");

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
