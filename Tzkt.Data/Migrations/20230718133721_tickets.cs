using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class tickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContractId",
                table: "Tickets",
                newName: "TicketerId");

            migrationBuilder.AddColumn<int>(
                name: "ContentHash",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContentTypeHash",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ContentTypeHash",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "TicketerId",
                table: "Tickets",
                newName: "ContractId");
        }
    }
}
