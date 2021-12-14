using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Tokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TokenTransfers",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenTransfers",
                table: "OriginationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenTransfers",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenBalanceCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenBalancesCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenTransfersCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokensCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveTokensCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenBalancesCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokenTransfersCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TokensCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    BalancesCount = table.Column<int>(type: "integer", nullable: false),
                    HoldersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMinted = table.Column<string>(type: "text", nullable: false),
                    TotalBurned = table.Column<string>(type: "text", nullable: false),
                    TotalSupply = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransfers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId",
                table: "TokenBalances",
                column: "AccountId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_Id",
                table: "TokenBalances",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_LastLevel",
                table: "TokenBalances",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_TokenId",
                table: "TokenBalances",
                column: "TokenId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ContractId",
                table: "Tokens",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ContractId_TokenId",
                table: "Tokens",
                columns: new[] { "ContractId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Id",
                table: "Tokens",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_LastLevel",
                table: "Tokens",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Metadata",
                table: "Tokens",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_FromId",
                table: "TokenTransfers",
                column: "FromId",
                filter: "\"FromId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_Id",
                table: "TokenTransfers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_Level",
                table: "TokenTransfers",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_MigrationId",
                table: "TokenTransfers",
                column: "MigrationId",
                filter: "\"MigrationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_OriginationId",
                table: "TokenTransfers",
                column: "OriginationId",
                filter: "\"OriginationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ToId",
                table: "TokenTransfers",
                column: "ToId",
                filter: "\"ToId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TokenId",
                table: "TokenTransfers",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TransactionId",
                table: "TokenTransfers",
                column: "TransactionId",
                filter: "\"TransactionId\" is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenBalances");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "TokenTransfers");

            migrationBuilder.DropColumn(
                name: "TokenTransfers",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "TokenTransfers",
                table: "OriginationOps");

            migrationBuilder.DropColumn(
                name: "TokenTransfers",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "TokenBalanceCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TokenBalancesCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TokenCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TokenTransfersCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TokensCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "ActiveTokensCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TokenBalancesCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TokenTransfersCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TokensCount",
                table: "Accounts");
        }
    }
}
