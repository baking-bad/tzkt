using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Proto24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "AIToggle",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "AIToggleEma",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "MaxBlockReward",
                table: "Cycles");

            migrationBuilder.AddColumn<int>(
                name: "AbaActivationLevel",
                table: "AppState",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                column: "AbaActivationLevel",
                value: null);

            migrationBuilder.RenameColumn(
                name: "BlockBonusPerSlot",
                table: "Cycles",
                newName: "BlockBonusPerBlock");

            migrationBuilder.Sql("""
                UPDATE "Cycles"
                SET "BlockBonusPerBlock" = "BlockBonusPerBlock" * 2333
                WHERE "BlockBonusPerBlock" != 0
                """);

            migrationBuilder.RenameColumn(
                name: "AttestationRewardPerSlot",
                table: "Cycles",
                newName: "AttestationRewardPerBlock");

            migrationBuilder.Sql("""
                UPDATE "Cycles"
                SET "AttestationRewardPerBlock" = "AttestationRewardPerBlock" * 7000
                WHERE "AttestationRewardPerBlock" != 0
                """);

            migrationBuilder.AddColumn<int>(
                name: "AddressRegistryIndex",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Index",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Index",
                table: "Accounts",
                column: "Index",
                unique: true,
                filter: "\"Index\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\r\nAND \"TokenTransfers\" IS NULL\r\nAND \"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Index",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AddressRegistryIndex",
                table: "TransactionOps");


            migrationBuilder.RenameColumn(
                name: "AttestationRewardPerBlock",
                table: "Cycles",
                newName: "AttestationRewardPerSlot");

            migrationBuilder.Sql("""
                UPDATE "Cycles"
                SET "AttestationRewardPerSlot" = "AttestationRewardPerSlot" / 7000
                WHERE "AttestationRewardPerSlot" != 0
                """);

            migrationBuilder.RenameColumn(
                name: "BlockBonusPerBlock",
                table: "Cycles",
                newName: "BlockBonusPerSlot");

            migrationBuilder.Sql("""
                UPDATE "Cycles"
                SET "BlockBonusPerSlot" = "BlockBonusPerSlot" / 2333
                WHERE "BlockBonusPerSlot" != 0
                """);

            migrationBuilder.DropColumn(
                name: "AbaActivationLevel",
                table: "AppState");

            migrationBuilder.AddColumn<long>(
                name: "MaxBlockReward",
                table: "Cycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "AIToggleEma",
                table: "Blocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AIToggle",
                table: "Blocks",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\nAND \"TokenTransfers\" IS NULL\nAND \"Status\" = 1");
        }
    }
}
