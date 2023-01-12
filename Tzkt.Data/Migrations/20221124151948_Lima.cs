using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Lima : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            void ToTimestamptz(string table, string column) =>
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ALTER ""{column}"" TYPE timestamptz USING ""{column}"" AT TIME ZONE 'UTC';");

            Triggers.RemoveNotificationTrigger(migrationBuilder, "sync_state_changed", "AppState");

            ToTimestamptz("VdfRevelationOps", "Timestamp");
            ToTimestamptz("TxRollupSubmitBatchOps", "Timestamp");
            ToTimestamptz("TxRollupReturnBondOps", "Timestamp");
            ToTimestamptz("TxRollupRemoveCommitmentOps", "Timestamp");
            ToTimestamptz("TxRollupRejectionOps", "Timestamp");
            ToTimestamptz("TxRollupOriginationOps", "Timestamp");
            ToTimestamptz("TxRollupFinalizeCommitmentOps", "Timestamp");
            ToTimestamptz("TxRollupDispatchTicketsOps", "Timestamp");
            ToTimestamptz("TxRollupCommitOps", "Timestamp");
            ToTimestamptz("TransferTicketOps", "Timestamp");
            ToTimestamptz("TransactionOps", "Timestamp");
            ToTimestamptz("Statistics", "Date");
            ToTimestamptz("SetDepositsLimitOps", "Timestamp");
            ToTimestamptz("RevelationPenaltyOps", "Timestamp");
            ToTimestamptz("RevealOps", "Timestamp");
            ToTimestamptz("RegisterConstantOps", "Timestamp");
            ToTimestamptz("Quotes", "Timestamp");
            ToTimestamptz("ProposalOps", "Timestamp");
            ToTimestamptz("PreendorsementOps", "Timestamp");
            ToTimestamptz("OriginationOps", "Timestamp");
            ToTimestamptz("NonceRevelationOps", "Timestamp");
            ToTimestamptz("MigrationOps", "Timestamp");
            ToTimestamptz("IncreasePaidStorageOps", "Timestamp");
            ToTimestamptz("EndorsingRewardOps", "Timestamp");
            ToTimestamptz("EndorsementOps", "Timestamp");
            ToTimestamptz("DoublePreendorsingOps", "Timestamp");
            ToTimestamptz("DoubleEndorsingOps", "Timestamp");
            ToTimestamptz("DoubleBakingOps", "Timestamp");
            ToTimestamptz("DelegationOps", "Timestamp");
            ToTimestamptz("Blocks", "Timestamp");
            ToTimestamptz("BallotOps", "Timestamp");
            ToTimestamptz("AppState", "Timestamp");
            ToTimestamptz("AppState", "LastSync");
            ToTimestamptz("ActivationOps", "Timestamp");

            Triggers.AddNotificationTrigger(migrationBuilder,
                name: "sync_state_changed",
                table: "AppState",
                column: "LastSync",
                payload: @"NEW.""KnownHead"" || ':' || NEW.""LastSync""");

            migrationBuilder.DropColumn(
                name: "LBSunsetLevel",
                table: "Protocols");

            migrationBuilder.AlterColumn<long>(
                name: "Operations",
                table: "Blocks",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "DrainDelegateOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdateConsensusKeyOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DrainDelegateCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdateConsensusKeyCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DrainDelegateOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Fee = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrainDelegateOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrainDelegateOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpdateConsensusKeyOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivationCycle = table.Column<int>(type: "integer", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    PublicKeyHash = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    BakerFee = table.Column<long>(type: "bigint", nullable: false),
                    StorageFee = table.Column<long>(type: "bigint", nullable: true),
                    AllocationFee = table.Column<long>(type: "bigint", nullable: true),
                    GasLimit = table.Column<int>(type: "integer", nullable: false),
                    GasUsed = table.Column<int>(type: "integer", nullable: false),
                    StorageLimit = table.Column<int>(type: "integer", nullable: false),
                    StorageUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Errors = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateConsensusKeyOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpdateConsensusKeyOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UpdateConsensusKeyOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrainDelegateOps_DelegateId",
                table: "DrainDelegateOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_DrainDelegateOps_Level",
                table: "DrainDelegateOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DrainDelegateOps_OpHash",
                table: "DrainDelegateOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_DrainDelegateOps_TargetId",
                table: "DrainDelegateOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateConsensusKeyOps_Level",
                table: "UpdateConsensusKeyOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateConsensusKeyOps_OpHash",
                table: "UpdateConsensusKeyOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateConsensusKeyOps_SenderId",
                table: "UpdateConsensusKeyOps",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            void ToTimestamp(string table, string column) =>
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ALTER ""{column}"" TYPE timestamp;");

            Triggers.RemoveNotificationTrigger(migrationBuilder, "sync_state_changed", "AppState");

            ToTimestamp("VdfRevelationOps", "Timestamp");
            ToTimestamp("TxRollupSubmitBatchOps", "Timestamp");
            ToTimestamp("TxRollupReturnBondOps", "Timestamp");
            ToTimestamp("TxRollupRemoveCommitmentOps", "Timestamp");
            ToTimestamp("TxRollupRejectionOps", "Timestamp");
            ToTimestamp("TxRollupOriginationOps", "Timestamp");
            ToTimestamp("TxRollupFinalizeCommitmentOps", "Timestamp");
            ToTimestamp("TxRollupDispatchTicketsOps", "Timestamp");
            ToTimestamp("TxRollupCommitOps", "Timestamp");
            ToTimestamp("TransferTicketOps", "Timestamp");
            ToTimestamp("TransactionOps", "Timestamp");
            ToTimestamp("Statistics", "Date");
            ToTimestamp("SetDepositsLimitOps", "Timestamp");
            ToTimestamp("RevelationPenaltyOps", "Timestamp");
            ToTimestamp("RevealOps", "Timestamp");
            ToTimestamp("RegisterConstantOps", "Timestamp");
            ToTimestamp("Quotes", "Timestamp");
            ToTimestamp("ProposalOps", "Timestamp");
            ToTimestamp("PreendorsementOps", "Timestamp");
            ToTimestamp("OriginationOps", "Timestamp");
            ToTimestamp("NonceRevelationOps", "Timestamp");
            ToTimestamp("MigrationOps", "Timestamp");
            ToTimestamp("IncreasePaidStorageOps", "Timestamp");
            ToTimestamp("EndorsingRewardOps", "Timestamp");
            ToTimestamp("EndorsementOps", "Timestamp");
            ToTimestamp("DoublePreendorsingOps", "Timestamp");
            ToTimestamp("DoubleEndorsingOps", "Timestamp");
            ToTimestamp("DoubleBakingOps", "Timestamp");
            ToTimestamp("DelegationOps", "Timestamp");
            ToTimestamp("Blocks", "Timestamp");
            ToTimestamp("BallotOps", "Timestamp");
            ToTimestamp("AppState", "Timestamp");
            ToTimestamp("AppState", "LastSync");
            ToTimestamp("ActivationOps", "Timestamp");

            Triggers.AddNotificationTrigger(migrationBuilder,
                name: "sync_state_changed",
                table: "AppState",
                column: "LastSync",
                payload: @"NEW.""KnownHead"" || ':' || NEW.""LastSync""");

            migrationBuilder.DropTable(
                name: "DrainDelegateOps");

            migrationBuilder.DropTable(
                name: "UpdateConsensusKeyOps");

            migrationBuilder.DropColumn(
                name: "DrainDelegateOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "UpdateConsensusKeyOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "DrainDelegateCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UpdateConsensusKeyCount",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "LBSunsetLevel",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Operations",
                table: "Blocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
