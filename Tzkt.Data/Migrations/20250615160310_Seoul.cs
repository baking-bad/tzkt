using Microsoft.EntityFrameworkCore.Migrations;
using Tzkt.Data.Models;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Seoul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UpdateConsensusKeyOps",
                newName: "UpdateSecondaryKeyOps");

            migrationBuilder.AddColumn<SecondaryKeyType>(
                name: "KeyType",
                table: "UpdateSecondaryKeyOps",
                type: "integer",
                nullable: false,
                defaultValue: SecondaryKeyType.Consensus);

            migrationBuilder.RenameColumn(
                name: "UpdateConsensusKeyOpsCount",
                table: "AppState",
                newName: "UpdateSecondaryKeyOpsCount");

            migrationBuilder.RenameColumn(
                name: "UpdateConsensusKeyCount",
                table: "Accounts",
                newName: "UpdateSecondaryKeyCount");

            migrationBuilder.AddColumn<int>(
                name: "StakerId",
                table: "StakingOps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "StakingOps"
                SET "StakerId" = "SenderId"
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StakingOps_StakerId",
                table: "StakingOps",
                column: "StakerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StakingOps_StakerId",
                table: "StakingOps");

            migrationBuilder.DropColumn(
                name: "StakerId",
                table: "StakingOps");

            migrationBuilder.RenameColumn(
                name: "UpdateSecondaryKeyCount",
                table: "Accounts",
                newName: "UpdateConsensusKeyCount");

            migrationBuilder.RenameColumn(
                name: "UpdateSecondaryKeyOpsCount",
                table: "AppState",
                newName: "UpdateConsensusKeyOpsCount");

            migrationBuilder.DropColumn(
                name: "KeyType",
                table: "UpdateSecondaryKeyOps");

            migrationBuilder.RenameTable(
                name: "UpdateSecondaryKeyOps",
                newName: "UpdateConsensusKeyOps");
        }
    }
}
