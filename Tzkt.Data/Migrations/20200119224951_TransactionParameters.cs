using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class TransactionParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Parameters",
                table: "TransactionOps",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "TransactionOps");
        }
    }
}
