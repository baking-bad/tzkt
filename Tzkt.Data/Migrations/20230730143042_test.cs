using System.Globalization;
using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Tickets");

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "Tickets",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ContentType",
                table: "Tickets",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId",
                table: "TicketBalances",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketerId",
                table: "TicketBalances",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketId",
                table: "TicketBalances",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketBalances_Accounts_AccountId",
                table: "TicketBalances",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketBalances_Accounts_TicketerId",
                table: "TicketBalances",
                column: "TicketerId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketBalances_Tickets_TicketId",
                table: "TicketBalances",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketBalances_Accounts_AccountId",
                table: "TicketBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketBalances_Accounts_TicketerId",
                table: "TicketBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketBalances_Tickets_TicketId",
                table: "TicketBalances");

            migrationBuilder.DropIndex(
                name: "IX_TicketBalances_AccountId",
                table: "TicketBalances");

            migrationBuilder.DropIndex(
                name: "IX_TicketBalances_TicketerId",
                table: "TicketBalances");

            migrationBuilder.DropIndex(
                name: "IX_TicketBalances_TicketId",
                table: "TicketBalances");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Tickets");

            migrationBuilder.AddColumn<BigInteger>(
                name: "TicketId",
                table: "Tickets",
                type: "numeric",
                nullable: false,
                defaultValue: BigInteger.Parse("0", NumberFormatInfo.InvariantInfo));
        }
    }
}
