using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class toDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "OriginationOps");

            migrationBuilder.RenameColumn(
                name: "OriginationId",
                table: "TicketTransfers",
                newName: "TransferTicketId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubIds",
                table: "TransferTicketOps");

            migrationBuilder.DropColumn(
                name: "TicketTransfers",
                table: "TransferTicketOps");

            migrationBuilder.RenameColumn(
                name: "TransferTicketId",
                table: "TicketTransfers",
                newName: "OriginationId");

            migrationBuilder.AddColumn<int>(
                name: "TicketTransfers",
                table: "OriginationOps",
                type: "integer",
                nullable: true);
        }
    }
}
