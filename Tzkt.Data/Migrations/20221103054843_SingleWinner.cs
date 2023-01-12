using Microsoft.EntityFrameworkCore.Migrations;
using Tzkt.Data.Models;

namespace Tzkt.Data.Migrations
{
    public partial class SingleWinner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SingleWinner",
                table: "VotingPeriods",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql($@"
                UPDATE ""VotingPeriods""
                SET ""SingleWinner"" = false
                WHERE ""Kind"" = {(int)PeriodKind.Proposal}
                AND ""TopVotingPower"" = 0");

            migrationBuilder.Sql($@"
                UPDATE ""VotingPeriods""
                SET ""SingleWinner"" = true
                WHERE ""Kind"" = {(int)PeriodKind.Proposal}
                AND ""TopVotingPower"" > 0");

            migrationBuilder.Sql($@"
                UPDATE ""VotingPeriods""
                SET ""Status"" = {(int)PeriodStatus.Success}
                WHERE ""Status"" = {(int)PeriodStatus.NoSingleWinner}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SingleWinner",
                table: "VotingPeriods");
        }
    }
}
