using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Attestations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("DoubleEndorsingOps",
                newName: "DoubleAttestationOps");

            migrationBuilder.RenameTable("DoublePreendorsingOps",
                newName: "DoublePreattestationOps");

            migrationBuilder.RenameTable("EndorsementOps",
                newName: "AttestationOps");

            migrationBuilder.RenameTable("EndorsingRewardOps",
                newName: "AttestationRewardOps");

            migrationBuilder.RenameTable("PreendorsementOps",
                newName: "PreattestationOps");


            migrationBuilder.DropIndex(
                name: "IX_StakingUpdates_DoubleEndorsingOpId",
                table: "StakingUpdates");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingOpId",
                table: "StakingUpdates",
                newName: "DoubleAttestationOpId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoubleAttestationOpId",
                table: "StakingUpdates",
                column: "DoubleAttestationOpId",
                filter: "\"DoubleAttestationOpId\" IS NOT NULL");


            migrationBuilder.DropIndex(
                name: "IX_StakingUpdates_DoublePreendorsingOpId",
                table: "StakingUpdates");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingOpId",
                table: "StakingUpdates",
                newName: "DoublePreattestationOpId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoublePreattestationOpId",
                table: "StakingUpdates",
                column: "DoublePreattestationOpId",
                filter: "\"DoublePreattestationOpId\" IS NOT NULL");


            migrationBuilder.RenameColumn(
                name: "MaxEndorsingReward",
                table: "Protocols",
                newName: "MaxAttestationReward");

            migrationBuilder.RenameColumn(
                name: "EndorsersPerBlock",
                table: "Protocols",
                newName: "AttestersPerBlock");

            migrationBuilder.RenameColumn(
                name: "EndorsementReward1",
                table: "Protocols",
                newName: "AttestationReward1");

            migrationBuilder.RenameColumn(
                name: "EndorsementReward0",
                table: "Protocols",
                newName: "AttestationReward0");

            migrationBuilder.RenameColumn(
                name: "EndorsementDeposit",
                table: "Protocols",
                newName: "AttestationDeposit");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingSlashedPercentage",
                table: "Protocols",
                newName: "DoubleAttestationSlashedPercentage");


            migrationBuilder.RenameColumn(
                name: "EndorsementRewardPerSlot",
                table: "Cycles",
                newName: "AttestationRewardPerSlot");


            migrationBuilder.RenameColumn(
                name: "MissedEndorsements",
                table: "BakerCycles",
                newName: "MissedAttestations");

            migrationBuilder.RenameColumn(
                name: "MissedEndorsementRewards",
                table: "BakerCycles",
                newName: "MissedAttestationRewards");

            migrationBuilder.RenameColumn(
                name: "FutureEndorsements",
                table: "BakerCycles",
                newName: "FutureAttestations");

            migrationBuilder.RenameColumn(
                name: "FutureEndorsementRewards",
                table: "BakerCycles",
                newName: "FutureAttestationRewards");

            migrationBuilder.RenameColumn(
                name: "ExpectedEndorsements",
                table: "BakerCycles",
                newName: "ExpectedAttestations");

            migrationBuilder.RenameColumn(
                name: "Endorsements",
                table: "BakerCycles",
                newName: "Attestations");

            migrationBuilder.RenameColumn(
                name: "EndorsementRewardsStakedShared",
                table: "BakerCycles",
                newName: "AttestationRewardsStakedShared");

            migrationBuilder.RenameColumn(
                name: "EndorsementRewardsStakedOwn",
                table: "BakerCycles",
                newName: "AttestationRewardsStakedOwn");

            migrationBuilder.RenameColumn(
                name: "EndorsementRewardsStakedEdge",
                table: "BakerCycles",
                newName: "AttestationRewardsStakedEdge");

            migrationBuilder.RenameColumn(
                name: "EndorsementRewardsDelegated",
                table: "BakerCycles",
                newName: "AttestationRewardsDelegated");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingRewards",
                table: "BakerCycles",
                newName: "DoublePreattestationRewards");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingLostUnstaked",
                table: "BakerCycles",
                newName: "DoublePreattestationLostUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingLostStaked",
                table: "BakerCycles",
                newName: "DoublePreattestationLostStaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingLostExternalUnstaked",
                table: "BakerCycles",
                newName: "DoublePreattestationLostExternalUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingLostExternalStaked",
                table: "BakerCycles",
                newName: "DoublePreattestationLostExternalStaked");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingRewards",
                table: "BakerCycles",
                newName: "DoubleAttestationRewards");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingLostUnstaked",
                table: "BakerCycles",
                newName: "DoubleAttestationLostUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingLostStaked",
                table: "BakerCycles",
                newName: "DoubleAttestationLostStaked");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingLostExternalUnstaked",
                table: "BakerCycles",
                newName: "DoubleAttestationLostExternalUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingLostExternalStaked",
                table: "BakerCycles",
                newName: "DoubleAttestationLostExternalStaked");


            migrationBuilder.RenameColumn(
                name: "PreendorsementOpsCount",
                table: "AppState",
                newName: "PreattestationOpsCount");

            migrationBuilder.RenameColumn(
                name: "EndorsingRewardOpsCount",
                table: "AppState",
                newName: "AttestationRewardOpsCount");

            migrationBuilder.RenameColumn(
                name: "EndorsementOpsCount",
                table: "AppState",
                newName: "AttestationOpsCount");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingOpsCount",
                table: "AppState",
                newName: "DoublePreattestationOpsCount");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingOpsCount",
                table: "AppState",
                newName: "DoubleAttestationOpsCount");


            migrationBuilder.RenameColumn(
                name: "PreendorsementsCount",
                table: "Accounts",
                newName: "PreattestationsCount");

            migrationBuilder.RenameColumn(
                name: "EndorsingRewardsCount",
                table: "Accounts",
                newName: "AttestationRewardsCount");

            migrationBuilder.RenameColumn(
                name: "EndorsementsCount",
                table: "Accounts",
                newName: "AttestationsCount");

            migrationBuilder.RenameColumn(
                name: "DoublePreendorsingCount",
                table: "Accounts",
                newName: "DoublePreattestationCount");

            migrationBuilder.RenameColumn(
                name: "DoubleEndorsingCount",
                table: "Accounts",
                newName: "DoubleAttestationCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("DoubleAttestationOps",
                newName: "DoubleEndorsingOps");

            migrationBuilder.RenameTable("DoublePreattestationOps",
                newName: "DoublePreendorsingOps");

            migrationBuilder.RenameTable("AttestationOps",
                newName: "EndorsementOps");

            migrationBuilder.RenameTable("AttestationRewardOps",
                newName: "EndorsingRewardOps");

            migrationBuilder.RenameTable("PreattestationOps",
                newName: "PreendorsementOps");


            migrationBuilder.DropIndex(
                name: "IX_StakingUpdates_DoubleAttestationOpId",
                table: "StakingUpdates");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationOpId",
                table: "StakingUpdates",
                newName: "DoubleEndorsingOpId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoubleEndorsingOpId",
                table: "StakingUpdates",
                column: "DoubleEndorsingOpId",
                filter: "\"DoubleEndorsingOpId\" IS NOT NULL");


            migrationBuilder.DropIndex(
                name: "IX_StakingUpdates_DoublePreattestationOpId",
                table: "StakingUpdates");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationOpId",
                table: "StakingUpdates",
                newName: "DoublePreendorsingOpId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoublePreendorsingOpId",
                table: "StakingUpdates",
                column: "DoublePreendorsingOpId",
                filter: "\"DoublePreendorsingOpId\" IS NOT NULL");


            migrationBuilder.RenameColumn(
                name: "MaxAttestationReward",
                table: "Protocols",
                newName: "MaxEndorsingReward");

            migrationBuilder.RenameColumn(
                name: "AttestersPerBlock",
                table: "Protocols",
                newName: "EndorsersPerBlock");

            migrationBuilder.RenameColumn(
                name: "AttestationReward1",
                table: "Protocols",
                newName: "EndorsementReward1");

            migrationBuilder.RenameColumn(
                name: "AttestationReward0",
                table: "Protocols",
                newName: "EndorsementReward0");

            migrationBuilder.RenameColumn(
                name: "AttestationDeposit",
                table: "Protocols",
                newName: "EndorsementDeposit");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationSlashedPercentage",
                table: "Protocols",
                newName: "DoubleEndorsingSlashedPercentage");


            migrationBuilder.RenameColumn(
                name: "AttestationRewardPerSlot",
                table: "Cycles",
                newName: "EndorsementRewardPerSlot");


            migrationBuilder.RenameColumn(
                name: "MissedAttestations",
                table: "BakerCycles",
                newName: "MissedEndorsements");

            migrationBuilder.RenameColumn(
                name: "MissedAttestationRewards",
                table: "BakerCycles",
                newName: "MissedEndorsementRewards");

            migrationBuilder.RenameColumn(
                name: "FutureAttestations",
                table: "BakerCycles",
                newName: "FutureEndorsements");

            migrationBuilder.RenameColumn(
                name: "FutureAttestationRewards",
                table: "BakerCycles",
                newName: "FutureEndorsementRewards");

            migrationBuilder.RenameColumn(
                name: "ExpectedAttestations",
                table: "BakerCycles",
                newName: "ExpectedEndorsements");

            migrationBuilder.RenameColumn(
                name: "Attestations",
                table: "BakerCycles",
                newName: "Endorsements");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardsStakedShared",
                table: "BakerCycles",
                newName: "EndorsementRewardsStakedShared");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardsStakedOwn",
                table: "BakerCycles",
                newName: "EndorsementRewardsStakedOwn");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardsStakedEdge",
                table: "BakerCycles",
                newName: "EndorsementRewardsStakedEdge");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardsDelegated",
                table: "BakerCycles",
                newName: "EndorsementRewardsDelegated");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationRewards",
                table: "BakerCycles",
                newName: "DoublePreendorsingRewards");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationLostUnstaked",
                table: "BakerCycles",
                newName: "DoublePreendorsingLostUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationLostStaked",
                table: "BakerCycles",
                newName: "DoublePreendorsingLostStaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationLostExternalUnstaked",
                table: "BakerCycles",
                newName: "DoublePreendorsingLostExternalUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationLostExternalStaked",
                table: "BakerCycles",
                newName: "DoublePreendorsingLostExternalStaked");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationRewards",
                table: "BakerCycles",
                newName: "DoubleEndorsingRewards");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationLostUnstaked",
                table: "BakerCycles",
                newName: "DoubleEndorsingLostUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationLostStaked",
                table: "BakerCycles",
                newName: "DoubleEndorsingLostStaked");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationLostExternalUnstaked",
                table: "BakerCycles",
                newName: "DoubleEndorsingLostExternalUnstaked");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationLostExternalStaked",
                table: "BakerCycles",
                newName: "DoubleEndorsingLostExternalStaked");


            migrationBuilder.RenameColumn(
                name: "PreattestationOpsCount",
                table: "AppState",
                newName: "PreendorsementOpsCount");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardOpsCount",
                table: "AppState",
                newName: "EndorsingRewardOpsCount");

            migrationBuilder.RenameColumn(
                name: "AttestationOpsCount",
                table: "AppState",
                newName: "EndorsementOpsCount");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationOpsCount",
                table: "AppState",
                newName: "DoublePreendorsingOpsCount");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationOpsCount",
                table: "AppState",
                newName: "DoubleEndorsingOpsCount");


            migrationBuilder.RenameColumn(
                name: "PreattestationsCount",
                table: "Accounts",
                newName: "PreendorsementsCount");

            migrationBuilder.RenameColumn(
                name: "AttestationRewardsCount",
                table: "Accounts",
                newName: "EndorsingRewardsCount");

            migrationBuilder.RenameColumn(
                name: "AttestationsCount",
                table: "Accounts",
                newName: "EndorsementsCount");

            migrationBuilder.RenameColumn(
                name: "DoublePreattestationCount",
                table: "Accounts",
                newName: "DoublePreendorsingCount");

            migrationBuilder.RenameColumn(
                name: "DoubleAttestationCount",
                table: "Accounts",
                newName: "DoubleEndorsingCount");
        }
    }
}
