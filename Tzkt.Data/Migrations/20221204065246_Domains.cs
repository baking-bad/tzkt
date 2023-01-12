using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Domains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DomainsLevel",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DomainsNameRegistry",
                table: "AppState",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Owner = table.Column<string>(type: "text", nullable: true),
                    Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "DomainsLevel", "DomainsNameRegistry" },
                values: new object[] { 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Address",
                table: "Domains",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_FirstLevel",
                table: "Domains",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_LastLevel",
                table: "Domains",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Level",
                table: "Domains",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Name",
                table: "Domains",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Owner",
                table: "Domains",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Domains");

            migrationBuilder.DropColumn(
                name: "DomainsLevel",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "DomainsNameRegistry",
                table: "AppState");
        }
    }
}
