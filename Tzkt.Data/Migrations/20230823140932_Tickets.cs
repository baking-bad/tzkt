using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubIds",
                table: "TransferTicketOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "TransferTicketOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubIds",
                table: "SmartRollupExecuteOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketBalancesCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfersCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActiveTicketsCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketBalancesCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfersCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    FirstMinterId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    BalancesCount = table.Column<int>(type: "integer", nullable: false),
                    HoldersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMinted = table.Column<string>(type: "text", nullable: false),
                    TotalBurned = table.Column<string>(type: "text", nullable: false),
                    TotalSupply = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<int>(type: "integer", nullable: false),
                    TypeHash = table.Column<int>(type: "integer", nullable: false),
                    RawContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    RawType = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonContent = table.Column<string>(type: "jsonb", nullable: true),
                    JsonType = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketTransfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    TransferTicketId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    SmartRollupExecuteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTransfers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "TicketBalancesCount", "TicketTransfersCount", "TicketsCount" },
                values: new object[] { 0, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId",
                table: "TicketBalances",
                column: "AccountId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId_TicketerId",
                table: "TicketBalances",
                columns: new[] { "AccountId", "TicketerId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId_TicketId",
                table: "TicketBalances",
                columns: new[] { "AccountId", "TicketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_Id",
                table: "TicketBalances",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_LastLevel",
                table: "TicketBalances",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketerId",
                table: "TicketBalances",
                column: "TicketerId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketId",
                table: "TicketBalances",
                column: "TicketId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_FirstMinterId",
                table: "Tickets",
                column: "FirstMinterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Id",
                table: "Tickets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LastLevel",
                table: "Tickets",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketerId",
                table: "Tickets",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_FromId",
                table: "TicketTransfers",
                column: "FromId",
                filter: "\"FromId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_Id",
                table: "TicketTransfers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_Level",
                table: "TicketTransfers",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_SmartRollupExecuteId",
                table: "TicketTransfers",
                column: "SmartRollupExecuteId",
                filter: "\"SmartRollupExecuteId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TicketerId",
                table: "TicketTransfers",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TicketId",
                table: "TicketTransfers",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_ToId",
                table: "TicketTransfers",
                column: "ToId",
                filter: "\"ToId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TransactionId",
                table: "TicketTransfers",
                column: "TransactionId",
                filter: "\"TransactionId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TransferTicketId",
                table: "TicketTransfers",
                column: "TransferTicketId",
                filter: "\"TransferTicketId\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketBalances");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketTransfers");

            migrationBuilder.DropColumn(
                name: "SubIds",
                table: "TransferTicketOps");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "TransferTicketOps");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "SubIds",
                table: "SmartRollupExecuteOps");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "TicketBalancesCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TicketTransfersCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TicketsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "ActiveTicketsCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TicketBalancesCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TicketTransfersCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TicketsCount",
                table: "Accounts");
        }
    }
}
