using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class tickets : Migration
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
                    Balance = table.Column<BigInteger>(type: "numeric", nullable: false),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
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
                    TotalMinted = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalBurned = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalSupply = table.Column<BigInteger>(type: "numeric", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true),
                    ContentHash = table.Column<int>(type: "integer", nullable: false),
                    ContentTypeHash = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    ContentType = table.Column<byte[]>(type: "bytea", nullable: true)
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
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    TransferTicketId = table.Column<long>(type: "bigint", nullable: true),
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
                columns: new[] { "TicketBalancesCount", "TicketTransfersCount", "TicketsCount" },
                values: new object[] { 0, 0, 0 });
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
