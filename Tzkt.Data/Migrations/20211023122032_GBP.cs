using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class GBP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Quotes""");
            migrationBuilder.Sql(@"UPDATE ""AppState"" SET ""QuoteLevel"" = -1");
            
            
            migrationBuilder.AddColumn<double>(
                name: "Gbp",
                table: "Quotes",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteGbp",
                table: "AppState",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gbp",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "QuoteGbp",
                table: "AppState");
        }
    }
}
