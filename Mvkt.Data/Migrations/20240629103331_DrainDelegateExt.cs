using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mvkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class DrainDelegateExt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AllocationFee",
                table: "DrainDelegateOps",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocationFee",
                table: "DrainDelegateOps");
        }
    }
}
