using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class VotingRolls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Redundant",
                table: "ProposalOps",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Rolls",
                table: "ProposalOps",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rolls",
                table: "BallotOps",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Redundant",
                table: "ProposalOps");

            migrationBuilder.DropColumn(
                name: "Rolls",
                table: "ProposalOps");

            migrationBuilder.DropColumn(
                name: "Rolls",
                table: "BallotOps");
        }
    }
}
