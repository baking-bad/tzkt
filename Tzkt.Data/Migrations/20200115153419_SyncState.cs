using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class SyncState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Synced",
                table: "AppState");

            migrationBuilder.AddColumn<int>(
                name: "KnownHead",
                table: "AppState",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSync",
                table: "AppState",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KnownHead",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "LastSync",
                table: "AppState");

            migrationBuilder.AddColumn<bool>(
                name: "Synced",
                table: "AppState",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
