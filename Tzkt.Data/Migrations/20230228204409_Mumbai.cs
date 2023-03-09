using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Mumbai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalSmartRollupBonds",
                table: "Statistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupChallengeWindow",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupCommitmentPeriod",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupOriginationSize",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SmartRollupStakeAmount",
                table: "Protocols",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupTimeoutPeriod",
                table: "Protocols",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InboxMessageCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RefutationGameCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupAddMessagesOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupCementOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupCommitmentCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupExecuteOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupOriginateOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupPublishOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupRecoverBondOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupRefuteOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "PublicKey",
                table: "Accounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(55)",
                oldMaxLength: 55,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveRefutationGamesCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CementedCommitments",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutedCommitments",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenesisCommitment",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InboxLevel",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastCommitment",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrphanCommitments",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingCommitments",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PvmKind",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefutationGamesCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RefutedCommitments",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupAddMessagesCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SmartRollupBonds",
                table: "Accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupCementCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupExecuteCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupOriginateCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupPublishCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupRecoverBondCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupRefuteCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SmartRollupsCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PredecessorLevel = table.Column<int>(type: "integer", nullable: true),
                    OperationId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefutationGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: false),
                    InitiatorId = table.Column<int>(type: "integer", nullable: false),
                    OpponentId = table.Column<int>(type: "integer", nullable: false),
                    InitiatorCommitmentId = table.Column<int>(type: "integer", nullable: false),
                    OpponentCommitmentId = table.Column<int>(type: "integer", nullable: false),
                    LastMoveId = table.Column<long>(type: "bigint", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    InitiatorReward = table.Column<long>(type: "bigint", nullable: true),
                    InitiatorLoss = table.Column<long>(type: "bigint", nullable: true),
                    OpponentReward = table.Column<long>(type: "bigint", nullable: true),
                    OpponentLoss = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefutationGames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupAddMessagesOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessagesCount = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_SmartRollupAddMessagesOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupAddMessagesOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupAddMessagesOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupCementOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
                    CommitmentId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SmartRollupCementOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupCementOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupCementOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupCommitments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InitiatorId = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: false),
                    PredecessorId = table.Column<int>(type: "integer", nullable: true),
                    InboxLevel = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "text", nullable: true),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    Ticks = table.Column<long>(type: "bigint", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Stakers = table.Column<int>(type: "integer", nullable: false),
                    ActiveStakers = table.Column<int>(type: "integer", nullable: false),
                    Successors = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartRollupCommitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupExecuteOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
                    CommitmentId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SmartRollupExecuteOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupExecuteOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupExecuteOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupOriginateOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PvmKind = table.Column<int>(type: "integer", nullable: false),
                    Kernel = table.Column<byte[]>(type: "bytea", nullable: true),
                    OriginationProof = table.Column<byte[]>(type: "bytea", nullable: true),
                    ParameterType = table.Column<byte[]>(type: "bytea", nullable: true),
                    GenesisCommitment = table.Column<string>(type: "text", nullable: true),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SmartRollupOriginateOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupOriginateOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupOriginateOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupPublishOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
                    CommitmentId = table.Column<int>(type: "integer", nullable: true),
                    Bond = table.Column<long>(type: "bigint", nullable: false),
                    BondStatus = table.Column<int>(type: "integer", nullable: true),
                    Flags = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_SmartRollupPublishOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupPublishOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupPublishOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupRecoverBondOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
                    StakerId = table.Column<int>(type: "integer", nullable: true),
                    Bond = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_SmartRollupRecoverBondOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupRecoverBondOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupRecoverBondOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupRefuteOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmartRollupId = table.Column<int>(type: "integer", nullable: true),
                    GameId = table.Column<int>(type: "integer", nullable: true),
                    Move = table.Column<int>(type: "integer", nullable: false),
                    GameStatus = table.Column<int>(type: "integer", nullable: false),
                    DissectionStart = table.Column<long>(type: "bigint", nullable: true),
                    DissectionEnd = table.Column<long>(type: "bigint", nullable: true),
                    DissectionSteps = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SmartRollupRefuteOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartRollupRefuteOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SmartRollupRefuteOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                columns: new[] { "InboxMessageCounter", "RefutationGameCounter", "SmartRollupAddMessagesOpsCount", "SmartRollupCementOpsCount", "SmartRollupCommitmentCounter", "SmartRollupExecuteOpsCount", "SmartRollupOriginateOpsCount", "SmartRollupPublishOpsCount", "SmartRollupRecoverBondOpsCount", "SmartRollupRefuteOpsCount" },
                values: new object[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Level",
                table: "InboxMessages",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_OperationId",
                table: "InboxMessages",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Type",
                table: "InboxMessages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_FirstLevel",
                table: "RefutationGames",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_InitiatorCommitmentId",
                table: "RefutationGames",
                column: "InitiatorCommitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_InitiatorId",
                table: "RefutationGames",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_LastLevel",
                table: "RefutationGames",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_OpponentCommitmentId",
                table: "RefutationGames",
                column: "OpponentCommitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_OpponentId",
                table: "RefutationGames",
                column: "OpponentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefutationGames_SmartRollupId",
                table: "RefutationGames",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupAddMessagesOps_Level",
                table: "SmartRollupAddMessagesOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupAddMessagesOps_OpHash",
                table: "SmartRollupAddMessagesOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupAddMessagesOps_SenderId",
                table: "SmartRollupAddMessagesOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCementOps_Level",
                table: "SmartRollupCementOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCementOps_OpHash",
                table: "SmartRollupCementOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCementOps_SenderId",
                table: "SmartRollupCementOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_Hash",
                table: "SmartRollupCommitments",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_Hash_SmartRollupId",
                table: "SmartRollupCommitments",
                columns: new[] { "Hash", "SmartRollupId" });

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_InboxLevel",
                table: "SmartRollupCommitments",
                column: "InboxLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_LastLevel",
                table: "SmartRollupCommitments",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_PredecessorId",
                table: "SmartRollupCommitments",
                column: "PredecessorId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupCommitments_SmartRollupId",
                table: "SmartRollupCommitments",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupExecuteOps_CommitmentId",
                table: "SmartRollupExecuteOps",
                column: "CommitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupExecuteOps_Level",
                table: "SmartRollupExecuteOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupExecuteOps_OpHash",
                table: "SmartRollupExecuteOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupExecuteOps_SenderId",
                table: "SmartRollupExecuteOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupExecuteOps_SmartRollupId",
                table: "SmartRollupExecuteOps",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupOriginateOps_Level",
                table: "SmartRollupOriginateOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupOriginateOps_OpHash",
                table: "SmartRollupOriginateOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupOriginateOps_SenderId",
                table: "SmartRollupOriginateOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupOriginateOps_SmartRollupId",
                table: "SmartRollupOriginateOps",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_CommitmentId",
                table: "SmartRollupPublishOps",
                column: "CommitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_Level",
                table: "SmartRollupPublishOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_OpHash",
                table: "SmartRollupPublishOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_SenderId",
                table: "SmartRollupPublishOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_SmartRollupId",
                table: "SmartRollupPublishOps",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_SmartRollupId_BondStatus_SenderId",
                table: "SmartRollupPublishOps",
                columns: new[] { "SmartRollupId", "BondStatus", "SenderId" },
                filter: "\"BondStatus\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRecoverBondOps_Level",
                table: "SmartRollupRecoverBondOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRecoverBondOps_OpHash",
                table: "SmartRollupRecoverBondOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRecoverBondOps_SenderId",
                table: "SmartRollupRecoverBondOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRecoverBondOps_SmartRollupId",
                table: "SmartRollupRecoverBondOps",
                column: "SmartRollupId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRecoverBondOps_StakerId",
                table: "SmartRollupRecoverBondOps",
                column: "StakerId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRefuteOps_GameId",
                table: "SmartRollupRefuteOps",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRefuteOps_Level",
                table: "SmartRollupRefuteOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRefuteOps_OpHash",
                table: "SmartRollupRefuteOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRefuteOps_SenderId",
                table: "SmartRollupRefuteOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupRefuteOps_SmartRollupId",
                table: "SmartRollupRefuteOps",
                column: "SmartRollupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "RefutationGames");

            migrationBuilder.DropTable(
                name: "SmartRollupAddMessagesOps");

            migrationBuilder.DropTable(
                name: "SmartRollupCementOps");

            migrationBuilder.DropTable(
                name: "SmartRollupCommitments");

            migrationBuilder.DropTable(
                name: "SmartRollupExecuteOps");

            migrationBuilder.DropTable(
                name: "SmartRollupOriginateOps");

            migrationBuilder.DropTable(
                name: "SmartRollupPublishOps");

            migrationBuilder.DropTable(
                name: "SmartRollupRecoverBondOps");

            migrationBuilder.DropTable(
                name: "SmartRollupRefuteOps");

            migrationBuilder.DropColumn(
                name: "TotalSmartRollupBonds",
                table: "Statistics");

            migrationBuilder.DropColumn(
                name: "SmartRollupChallengeWindow",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "SmartRollupCommitmentPeriod",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "SmartRollupOriginationSize",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "SmartRollupStakeAmount",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "SmartRollupTimeoutPeriod",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "InboxMessageCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "RefutationGameCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupAddMessagesOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupCementOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupCommitmentCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupExecuteOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupOriginateOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupPublishOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupRecoverBondOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "SmartRollupRefuteOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "ActiveRefutationGamesCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CementedCommitments",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ExecutedCommitments",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "GenesisCommitment",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "InboxLevel",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastCommitment",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OrphanCommitments",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PendingCommitments",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PvmKind",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "RefutationGamesCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "RefutedCommitments",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupAddMessagesCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupBonds",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupCementCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupExecuteCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupOriginateCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupPublishCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupRecoverBondCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupRefuteCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SmartRollupsCount",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "PublicKey",
                table: "Accounts",
                type: "character varying(55)",
                maxLength: 55,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
