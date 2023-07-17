using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class ticket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    TicketId = table.Column<BigInteger>(type: "numeric", nullable: false),
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
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                column: "TicketsCount",
                value: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_TransferTicketOps_Accounts_TicketerId",
                table: "TransferTicketOps",
                column: "TicketerId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransferTicketOps_Accounts_TicketerId",
                table: "TransferTicketOps");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "TicketsCount",
                table: "Accounts");
        }
    }
}
