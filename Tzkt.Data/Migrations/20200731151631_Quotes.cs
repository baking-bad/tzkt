using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Quotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "QuoteBtc",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteEur",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "QuoteLevel",
                table: "AppState",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteUsd",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Btc = table.Column<double>(nullable: false),
                    Eur = table.Column<double>(nullable: false),
                    Usd = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                column: "QuoteLevel",
                value: -1);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Level",
                table: "Quotes",
                column: "Level",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropColumn(
                name: "QuoteBtc",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteEur",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteLevel",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteUsd",
                table: "AppState");
        }
    }
}
