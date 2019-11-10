using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class DoubleEndorsing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffenderLoss",
                table: "DoubleEndorsingOps");

            migrationBuilder.AddColumn<long>(
                name: "OffenderLostDeposit",
                table: "DoubleEndorsingOps",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OffenderLostFee",
                table: "DoubleEndorsingOps",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OffenderLostReward",
                table: "DoubleEndorsingOps",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffenderLostDeposit",
                table: "DoubleEndorsingOps");

            migrationBuilder.DropColumn(
                name: "OffenderLostFee",
                table: "DoubleEndorsingOps");

            migrationBuilder.DropColumn(
                name: "OffenderLostReward",
                table: "DoubleEndorsingOps");

            migrationBuilder.AddColumn<long>(
                name: "OffenderLoss",
                table: "DoubleEndorsingOps",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
