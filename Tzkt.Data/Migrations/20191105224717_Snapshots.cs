using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Snapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VotingSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    PeriodId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Rolls = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingSnapshots_VotingPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_Level",
                table: "VotingSnapshots",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_PeriodId_DelegateId",
                table: "VotingSnapshots",
                columns: new[] { "PeriodId", "DelegateId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VotingSnapshots");
        }
    }
}
