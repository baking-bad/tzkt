using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Supply : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Cny",
                table: "Quotes",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Jpy",
                table: "Quotes",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Krw",
                table: "Quotes",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<long>(
                name: "Amount",
                table: "DelegationOps",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SoftwareId",
                table: "Blocks",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommitmentsCount",
                table: "AppState",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteCny",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteJpy",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteKrw",
                table: "AppState",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SoftwareId",
                table: "Accounts",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Commitments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(fixedLength: true, maxLength: 37, nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    AccountId = table.Column<int>(nullable: true),
                    Level = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Software",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlocksCount = table.Column<int>(nullable: false),
                    FirstLevel = table.Column<int>(nullable: false),
                    LastLevel = table.Column<int>(nullable: false),
                    ShortHash = table.Column<string>(fixedLength: true, maxLength: 8, nullable: false),
                    CommitDate = table.Column<DateTime>(nullable: true),
                    CommitHash = table.Column<string>(fixedLength: true, maxLength: 40, nullable: true),
                    Version = table.Column<string>(nullable: true),
                    Tags = table.Column<List<string>>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Cycle = table.Column<int>(nullable: true),
                    Date = table.Column<DateTime>(nullable: true),
                    TotalBootstrapped = table.Column<long>(nullable: false),
                    TotalCommitments = table.Column<long>(nullable: false),
                    TotalActivated = table.Column<long>(nullable: false),
                    TotalCreated = table.Column<long>(nullable: false),
                    TotalBurned = table.Column<long>(nullable: false),
                    TotalVested = table.Column<long>(nullable: false),
                    TotalFrozen = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SoftwareId",
                table: "Blocks",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SoftwareId",
                table: "Accounts",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_Address",
                table: "Commitments",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_Id",
                table: "Commitments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Cycle",
                table: "Statistics",
                column: "Cycle",
                unique: true,
                filter: "\"Cycle\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Date",
                table: "Statistics",
                column: "Date",
                unique: true,
                filter: "\"Date\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Level",
                table: "Statistics",
                column: "Level",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Software_SoftwareId",
                table: "Accounts",
                column: "SoftwareId",
                principalTable: "Software",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Software_SoftwareId",
                table: "Blocks",
                column: "SoftwareId",
                principalTable: "Software",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Software_SoftwareId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Software_SoftwareId",
                table: "Blocks");

            migrationBuilder.DropTable(
                name: "Commitments");

            migrationBuilder.DropTable(
                name: "Software");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_SoftwareId",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_SoftwareId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Cny",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Jpy",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Krw",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "DelegationOps");

            migrationBuilder.DropColumn(
                name: "SoftwareId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "CommitmentsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteCny",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteJpy",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteKrw",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SoftwareId",
                table: "Accounts");
        }
    }
}
