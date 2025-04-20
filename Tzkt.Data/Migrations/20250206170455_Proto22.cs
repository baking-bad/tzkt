using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Proto22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxSlashingPeriod",
                table: "Protocols",
                newName: "ToleratedInactivityPeriod");

            migrationBuilder.AddColumn<int>(
                name: "DenunciationPeriod",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfShards",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlashingDelay",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "DalAttestationRewardPerShard",
                table: "Cycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DalAttestationRewardsDelegated",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DalAttestationRewardsStakedEdge",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DalAttestationRewardsStakedOwn",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DalAttestationRewardsStakedShared",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ExpectedDalShards",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FutureDalAttestationRewards",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MissedDalAttestationRewards",
                table: "BakerCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "DalAttestationRewardOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalEntrapmentEvidenceOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DalAttestationRewardsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DalEntrapmentEvidenceOpsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DalAttestationRewardOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Expected = table.Column<long>(type: "bigint", nullable: false),
                    RewardDelegated = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedShared = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalAttestationRewardOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DalEntrapmentEvidenceOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    TrapLevel = table.Column<int>(type: "integer", nullable: false),
                    TrapSlotIndex = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DalEntrapmentEvidenceOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DalEntrapmentEvidenceOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "DalAttestationRewardOpsCount", "DalEntrapmentEvidenceOpsCount" },
                values: new object[] { 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestationRewardOps_BakerId",
                table: "DalAttestationRewardOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DalAttestationRewardOps_Level",
                table: "DalAttestationRewardOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DalEntrapmentEvidenceOps_AccuserId",
                table: "DalEntrapmentEvidenceOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DalEntrapmentEvidenceOps_Level",
                table: "DalEntrapmentEvidenceOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DalEntrapmentEvidenceOps_OffenderId",
                table: "DalEntrapmentEvidenceOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DalEntrapmentEvidenceOps_OpHash",
                table: "DalEntrapmentEvidenceOps",
                column: "OpHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DalAttestationRewardOps");

            migrationBuilder.DropTable(
                name: "DalEntrapmentEvidenceOps");

            migrationBuilder.DropColumn(
                name: "DenunciationPeriod",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "NumberOfShards",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "SlashingDelay",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardPerShard",
                table: "Cycles");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardsDelegated",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardsStakedEdge",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardsStakedOwn",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardsStakedShared",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "ExpectedDalShards",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "FutureDalAttestationRewards",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "MissedDalAttestationRewards",
                table: "BakerCycles");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "DalEntrapmentEvidenceOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "DalAttestationRewardsCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DalEntrapmentEvidenceOpsCount",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "ToleratedInactivityPeriod",
                table: "Protocols",
                newName: "MaxSlashingPeriod");
        }
    }
}
