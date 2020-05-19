using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Baking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountCounter",
                table: "AppState",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BakerCycles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Rolls = table.Column<int>(nullable: false),
                    StakingBalance = table.Column<long>(nullable: false),
                    DelegatedBalance = table.Column<long>(nullable: false),
                    DelegatorsCount = table.Column<int>(nullable: false),
                    FutureBlocks = table.Column<int>(nullable: false),
                    OwnBlocks = table.Column<int>(nullable: false),
                    ExtraBlocks = table.Column<int>(nullable: false),
                    MissedOwnBlocks = table.Column<int>(nullable: false),
                    MissedExtraBlocks = table.Column<int>(nullable: false),
                    UncoveredOwnBlocks = table.Column<int>(nullable: false),
                    UncoveredExtraBlocks = table.Column<int>(nullable: false),
                    FutureEndorsements = table.Column<int>(nullable: false),
                    Endorsements = table.Column<int>(nullable: false),
                    MissedEndorsements = table.Column<int>(nullable: false),
                    UncoveredEndorsements = table.Column<int>(nullable: false),
                    FutureBlockRewards = table.Column<long>(nullable: false),
                    OwnBlockRewards = table.Column<long>(nullable: false),
                    ExtraBlockRewards = table.Column<long>(nullable: false),
                    MissedOwnBlockRewards = table.Column<long>(nullable: false),
                    MissedExtraBlockRewards = table.Column<long>(nullable: false),
                    UncoveredOwnBlockRewards = table.Column<long>(nullable: false),
                    UncoveredExtraBlockRewards = table.Column<long>(nullable: false),
                    FutureEndorsementRewards = table.Column<long>(nullable: false),
                    EndorsementRewards = table.Column<long>(nullable: false),
                    MissedEndorsementRewards = table.Column<long>(nullable: false),
                    UncoveredEndorsementRewards = table.Column<long>(nullable: false),
                    OwnBlockFees = table.Column<long>(nullable: false),
                    ExtraBlockFees = table.Column<long>(nullable: false),
                    MissedOwnBlockFees = table.Column<long>(nullable: false),
                    MissedExtraBlockFees = table.Column<long>(nullable: false),
                    UncoveredOwnBlockFees = table.Column<long>(nullable: false),
                    UncoveredExtraBlockFees = table.Column<long>(nullable: false),
                    AccusationRewards = table.Column<long>(nullable: false),
                    AccusationLostDeposits = table.Column<long>(nullable: false),
                    AccusationLostRewards = table.Column<long>(nullable: false),
                    AccusationLostFees = table.Column<long>(nullable: false),
                    RevelationRewards = table.Column<long>(nullable: false),
                    RevelationLostRewards = table.Column<long>(nullable: false),
                    RevelationLostFees = table.Column<long>(nullable: false),
                    FutureBlockDeposits = table.Column<long>(nullable: false),
                    BlockDeposits = table.Column<long>(nullable: false),
                    FutureEndorsementDeposits = table.Column<long>(nullable: false),
                    EndorsementDeposits = table.Column<long>(nullable: false),
                    ExpectedBlocks = table.Column<double>(nullable: false),
                    ExpectedEndorsements = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakerCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cycles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<int>(nullable: false),
                    SnapshotLevel = table.Column<int>(nullable: false),
                    TotalRolls = table.Column<int>(nullable: false),
                    TotalStaking = table.Column<long>(nullable: false),
                    TotalDelegated = table.Column<long>(nullable: false),
                    TotalDelegators = table.Column<int>(nullable: false),
                    TotalBakers = table.Column<int>(nullable: false),
                    Seed = table.Column<string>(fixedLength: true, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                    table.UniqueConstraint("AK_Cycles_Index", x => x.Index);
                });

            migrationBuilder.CreateTable(
                name: "DelegatorCycles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(nullable: false),
                    DelegatorId = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatorCycles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_BakerId",
                table: "BakerCycles",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Cycle",
                table: "BakerCycles",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Id",
                table: "BakerCycles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Cycle_BakerId",
                table: "BakerCycles",
                columns: new[] { "Cycle", "BakerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_Index",
                table: "Cycles",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle",
                table: "DelegatorCycles",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_DelegatorId",
                table: "DelegatorCycles",
                column: "DelegatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_BakerId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "BakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_DelegatorId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "DelegatorId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BakerCycles");

            migrationBuilder.DropTable(
                name: "Cycles");

            migrationBuilder.DropTable(
                name: "DelegatorCycles");

            migrationBuilder.DropColumn(
                name: "AccountCounter",
                table: "AppState");
        }
    }
}
