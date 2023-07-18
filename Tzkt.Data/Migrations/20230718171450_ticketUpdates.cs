using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class ticketUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "OriginationOps",
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
                    Balance = table.Column<BigInteger>(type: "numeric", nullable: false),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketBalances", x => x.Id);
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
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    OriginationId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    MigrationId = table.Column<long>(type: "bigint", nullable: true),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTransfers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "TicketBalancesCount", "TicketTransfersCount" },
                values: new object[] { 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketBalances");

            migrationBuilder.DropTable(
                name: "TicketTransfers");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "OriginationOps");

            migrationBuilder.DropColumn(
                name: "TicketBalancesCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TicketTransfersCount",
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
        }
    }
}
