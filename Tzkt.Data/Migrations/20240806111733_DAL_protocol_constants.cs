using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class DAL_protocol_constants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DalAttestationLag",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalAttestationThreshold",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalShardsPerSlot",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalSlotsPerLevel",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DalAttestationLag",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalAttestationThreshold",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalShardsPerSlot",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalSlotsPerLevel",
                table: "Protocols");
        }
    }
}
