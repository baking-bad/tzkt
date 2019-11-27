using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppState",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Synced = table.Column<bool>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Protocol = table.Column<string>(nullable: true),
                    NextProtocol = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true),
                    GlobalCounter = table.Column<int>(nullable: false),
                    ManagerCounter = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    FirstLevel = table.Column<int>(nullable: false),
                    LastLevel = table.Column<int>(nullable: false),
                    PreserverCycles = table.Column<int>(nullable: false),
                    BlocksPerCycle = table.Column<int>(nullable: false),
                    BlocksPerCommitment = table.Column<int>(nullable: false),
                    BlocksPerSnapshot = table.Column<int>(nullable: false),
                    BlocksPerVoting = table.Column<int>(nullable: false),
                    TimeBetweenBlocks = table.Column<int>(nullable: false),
                    EndorsersPerBlock = table.Column<int>(nullable: false),
                    HardOperationGasLimit = table.Column<int>(nullable: false),
                    HardOperationStorageLimit = table.Column<int>(nullable: false),
                    HardBlockGasLimit = table.Column<int>(nullable: false),
                    TokensPerRoll = table.Column<long>(nullable: false),
                    RevelationReward = table.Column<long>(nullable: false),
                    BlockDeposit = table.Column<long>(nullable: false),
                    BlockReward = table.Column<long>(nullable: false),
                    EndorsementDeposit = table.Column<long>(nullable: false),
                    EndorsementReward = table.Column<long>(nullable: false),
                    OriginationSize = table.Column<int>(nullable: false),
                    ByteCost = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                    table.UniqueConstraint("AK_Protocols_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "VotingEpoches",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Progress = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingEpoches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VotingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<int>(nullable: false),
                    EpochId = table.Column<int>(nullable: false),
                    Kind = table.Column<int>(nullable: false),
                    StartLevel = table.Column<int>(nullable: false),
                    EndLevel = table.Column<int>(nullable: false),
                    ProposalId = table.Column<int>(nullable: true),
                    TotalStake = table.Column<int>(nullable: true),
                    Participation = table.Column<int>(nullable: true),
                    Quorum = table.Column<int>(nullable: true),
                    Abstainings = table.Column<int>(nullable: true),
                    Approvals = table.Column<int>(nullable: true),
                    Refusals = table.Column<int>(nullable: true),
                    PromotionPeriod_ProposalId = table.Column<int>(nullable: true),
                    PromotionPeriod_TotalStake = table.Column<int>(nullable: true),
                    PromotionPeriod_Participation = table.Column<int>(nullable: true),
                    PromotionPeriod_Quorum = table.Column<int>(nullable: true),
                    PromotionPeriod_Abstainings = table.Column<int>(nullable: true),
                    PromotionPeriod_Approvals = table.Column<int>(nullable: true),
                    PromotionPeriod_Refusals = table.Column<int>(nullable: true),
                    TestingPeriod_ProposalId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingPeriods_VotingEpoches_EpochId",
                        column: x => x.EpochId,
                        principalTable: "VotingEpoches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "ActivationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    AccountId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BallotOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    PeriodId = table.Column<int>(nullable: false),
                    ProposalId = table.Column<int>(nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Vote = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BallotOps_VotingPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    ProtoCode = table.Column<int>(nullable: false),
                    Priority = table.Column<int>(nullable: false),
                    Validations = table.Column<int>(nullable: false),
                    Events = table.Column<int>(nullable: false),
                    Operations = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: true),
                    RevelationId = table.Column<int>(nullable: true),
                    ResetDeactivation = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.UniqueConstraint("AK_Blocks_Level", x => x.Level);
                    table.ForeignKey(
                        name: "FK_Blocks_Protocols_ProtoCode",
                        column: x => x.ProtoCode,
                        principalTable: "Protocols",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(fixedLength: true, maxLength: 36, nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    FirstLevel = table.Column<int>(nullable: false),
                    LastLevel = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    Contracts = table.Column<int>(nullable: false),
                    DelegationsCount = table.Column<int>(nullable: false),
                    OriginationsCount = table.Column<int>(nullable: false),
                    TransactionsCount = table.Column<int>(nullable: false),
                    RevealsCount = table.Column<int>(nullable: false),
                    SystemOpsCount = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true),
                    DelegationLevel = table.Column<int>(nullable: true),
                    Staked = table.Column<bool>(nullable: false),
                    Kind = table.Column<byte>(nullable: true),
                    Spendable = table.Column<bool>(nullable: true),
                    CreatorId = table.Column<int>(nullable: true),
                    ManagerId = table.Column<int>(nullable: true),
                    WeirdDelegateId = table.Column<int>(nullable: true),
                    Activated = table.Column<bool>(nullable: true),
                    PublicKey = table.Column<string>(maxLength: 55, nullable: true),
                    ActivationLevel = table.Column<int>(nullable: true),
                    DeactivationLevel = table.Column<int>(nullable: true),
                    FrozenDeposits = table.Column<long>(nullable: true),
                    FrozenRewards = table.Column<long>(nullable: true),
                    FrozenFees = table.Column<long>(nullable: true),
                    Delegators = table.Column<int>(nullable: true),
                    StakingBalance = table.Column<long>(nullable: true),
                    EndorsementsCount = table.Column<int>(nullable: true),
                    BallotsCount = table.Column<int>(nullable: true),
                    ProposalsCount = table.Column<int>(nullable: true),
                    DoubleBakingCount = table.Column<int>(nullable: true),
                    DoubleEndorsingCount = table.Column<int>(nullable: true),
                    NonceRevelationsCount = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Blocks_FirstLevel",
                        column: x => x.FirstLevel,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_WeirdDelegateId",
                        column: x => x.WeirdDelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoubleBakingOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    AccusedLevel = table.Column<int>(nullable: false),
                    AccuserId = table.Column<int>(nullable: false),
                    AccuserReward = table.Column<long>(nullable: false),
                    OffenderId = table.Column<int>(nullable: false),
                    OffenderLostDeposit = table.Column<long>(nullable: false),
                    OffenderLostReward = table.Column<long>(nullable: false),
                    OffenderLostFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleBakingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Accounts_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Accounts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoubleEndorsingOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    AccusedLevel = table.Column<int>(nullable: false),
                    AccuserId = table.Column<int>(nullable: false),
                    AccuserReward = table.Column<long>(nullable: false),
                    OffenderId = table.Column<int>(nullable: false),
                    OffenderLostDeposit = table.Column<long>(nullable: false),
                    OffenderLostReward = table.Column<long>(nullable: false),
                    OffenderLostFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleEndorsingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Accounts_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Accounts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsementOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Slots = table.Column<int>(nullable: false),
                    Reward = table.Column<long>(nullable: false),
                    ResetDeactivation = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsementOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndorsementOps_Accounts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EndorsementOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NonceRevelationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    RevealedLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonceRevelationOps", x => x.Id);
                    table.UniqueConstraint("AK_NonceRevelationOps_RevealedLevel", x => x.RevealedLevel);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Accounts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: true),
                    Status = table.Column<int>(nullable: false),
                    InitiatorId = table.Column<int>(nullable: false),
                    Likes = table.Column<int>(nullable: false),
                    ProposalPeriodId = table.Column<int>(nullable: false),
                    ExplorationPeriodId = table.Column<int>(nullable: true),
                    TestingPeriodId = table.Column<int>(nullable: true),
                    PromotionPeriodId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposals_VotingPeriods_ExplorationPeriodId",
                        column: x => x.ExplorationPeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proposals_Accounts_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proposals_VotingPeriods_PromotionPeriodId",
                        column: x => x.PromotionPeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proposals_VotingPeriods_ProposalPeriodId",
                        column: x => x.ProposalPeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proposals_VotingPeriods_TestingPeriodId",
                        column: x => x.TestingPeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevealOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    BakerFee = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: true),
                    AllocationFee = table.Column<long>(nullable: true),
                    GasLimit = table.Column<int>(nullable: false),
                    GasUsed = table.Column<int>(nullable: false),
                    StorageLimit = table.Column<int>(nullable: false),
                    StorageUsed = table.Column<int>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    Errors = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevealOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevealOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevealOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    AccountId = table.Column<int>(nullable: false),
                    Event = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemOps_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    BakerFee = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: true),
                    AllocationFee = table.Column<long>(nullable: true),
                    GasLimit = table.Column<int>(nullable: false),
                    GasUsed = table.Column<int>(nullable: false),
                    StorageLimit = table.Column<int>(nullable: false),
                    StorageUsed = table.Column<int>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    Errors = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    TargetId = table.Column<int>(nullable: true),
                    ResetDeactivation = table.Column<int>(nullable: true),
                    Amount = table.Column<long>(nullable: false),
                    InternalOperations = table.Column<byte>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOps_TransactionOps_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TransactionOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Accounts_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProposalOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    PeriodId = table.Column<int>(nullable: false),
                    ProposalId = table.Column<int>(nullable: false),
                    SenderId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_VotingPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "VotingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DelegationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    BakerFee = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: true),
                    AllocationFee = table.Column<long>(nullable: true),
                    GasLimit = table.Column<int>(nullable: false),
                    GasUsed = table.Column<int>(nullable: false),
                    StorageLimit = table.Column<int>(nullable: false),
                    StorageUsed = table.Column<int>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    Errors = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    DelegateId = table.Column<int>(nullable: true),
                    ResetDeactivation = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Accounts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DelegationOps_TransactionOps_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TransactionOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OriginationOps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    OpHash = table.Column<string>(fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(nullable: false),
                    Counter = table.Column<int>(nullable: false),
                    BakerFee = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: true),
                    AllocationFee = table.Column<long>(nullable: true),
                    GasLimit = table.Column<int>(nullable: false),
                    GasUsed = table.Column<int>(nullable: false),
                    StorageLimit = table.Column<int>(nullable: false),
                    StorageUsed = table.Column<int>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    Errors = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    ManagerId = table.Column<int>(nullable: true),
                    DelegateId = table.Column<int>(nullable: true),
                    ContractId = table.Column<int>(nullable: true),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_TransactionOps_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TransactionOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "GlobalCounter", "Hash", "Level", "ManagerCounter", "NextProtocol", "Protocol", "Synced", "Timestamp" },
                values: new object[] { -1, 0, "", -1, 0, "", "", false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Address",
                table: "Accounts",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DelegateId",
                table: "Accounts",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FirstLevel",
                table: "Accounts",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Id",
                table: "Accounts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatorId",
                table: "Accounts",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ManagerId",
                table: "Accounts",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_WeirdDelegateId",
                table: "Accounts",
                column: "WeirdDelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationOps_AccountId",
                table: "ActivationOps",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivationOps_Level",
                table: "ActivationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationOps_OpHash",
                table: "ActivationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_Level",
                table: "BallotOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_OpHash",
                table: "BallotOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_PeriodId",
                table: "BallotOps",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_ProposalId",
                table: "BallotOps",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_SenderId",
                table: "BallotOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BakerId",
                table: "Blocks",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Hash",
                table: "Blocks",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Level",
                table: "Blocks",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ProtoCode",
                table: "Blocks",
                column: "ProtoCode");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_RevelationId",
                table: "Blocks",
                column: "RevelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_DelegateId",
                table: "DelegationOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_Level",
                table: "DelegationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_OpHash",
                table: "DelegationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_ParentId",
                table: "DelegationOps",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_SenderId",
                table: "DelegationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_AccuserId",
                table: "DoubleBakingOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_Level",
                table: "DoubleBakingOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_OffenderId",
                table: "DoubleBakingOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleBakingOps_OpHash",
                table: "DoubleBakingOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_AccuserId",
                table: "DoubleEndorsingOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_Level",
                table: "DoubleEndorsingOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_OffenderId",
                table: "DoubleEndorsingOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DoubleEndorsingOps_OpHash",
                table: "DoubleEndorsingOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsementOps_DelegateId",
                table: "EndorsementOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsementOps_Level",
                table: "EndorsementOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsementOps_OpHash",
                table: "EndorsementOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_BakerId",
                table: "NonceRevelationOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_Level",
                table: "NonceRevelationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_OpHash",
                table: "NonceRevelationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_SenderId",
                table: "NonceRevelationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ContractId",
                table: "OriginationOps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_DelegateId",
                table: "OriginationOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_Level",
                table: "OriginationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ManagerId",
                table: "OriginationOps",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_OpHash",
                table: "OriginationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ParentId",
                table: "OriginationOps",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_SenderId",
                table: "OriginationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_Level",
                table: "ProposalOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_OpHash",
                table: "ProposalOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_PeriodId",
                table: "ProposalOps",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_ProposalId",
                table: "ProposalOps",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_SenderId",
                table: "ProposalOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ExplorationPeriodId",
                table: "Proposals",
                column: "ExplorationPeriodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_InitiatorId",
                table: "Proposals",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_PromotionPeriodId",
                table: "Proposals",
                column: "PromotionPeriodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ProposalPeriodId",
                table: "Proposals",
                column: "ProposalPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_TestingPeriodId",
                table: "Proposals",
                column: "TestingPeriodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevealOps_Level",
                table: "RevealOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_RevealOps_OpHash",
                table: "RevealOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_RevealOps_SenderId",
                table: "RevealOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOps_AccountId",
                table: "SystemOps",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOps_Level",
                table: "SystemOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_Level",
                table: "TransactionOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_OpHash",
                table: "TransactionOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_ParentId",
                table: "TransactionOps",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_SenderId",
                table: "TransactionOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId",
                table: "TransactionOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_EpochId",
                table: "VotingPeriods",
                column: "EpochId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_Level",
                table: "VotingSnapshots",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_PeriodId_DelegateId",
                table: "VotingSnapshots",
                columns: new[] { "PeriodId", "DelegateId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationOps_Blocks_Level",
                table: "ActivationOps",
                column: "Level",
                principalTable: "Blocks",
                principalColumn: "Level",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationOps_Accounts_AccountId",
                table: "ActivationOps",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BallotOps_Blocks_Level",
                table: "BallotOps",
                column: "Level",
                principalTable: "Blocks",
                principalColumn: "Level",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BallotOps_Accounts_SenderId",
                table: "BallotOps",
                column: "SenderId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BallotOps_Proposals_ProposalId",
                table: "BallotOps",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Accounts_BakerId",
                table: "Blocks",
                column: "BakerId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_NonceRevelationOps_RevelationId",
                table: "Blocks",
                column: "RevelationId",
                principalTable: "NonceRevelationOps",
                principalColumn: "RevealedLevel",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Blocks_FirstLevel",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_NonceRevelationOps_Blocks_Level",
                table: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "ActivationOps");

            migrationBuilder.DropTable(
                name: "AppState");

            migrationBuilder.DropTable(
                name: "BallotOps");

            migrationBuilder.DropTable(
                name: "DelegationOps");

            migrationBuilder.DropTable(
                name: "DoubleBakingOps");

            migrationBuilder.DropTable(
                name: "DoubleEndorsingOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "SystemOps");

            migrationBuilder.DropTable(
                name: "VotingSnapshots");

            migrationBuilder.DropTable(
                name: "TransactionOps");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "VotingPeriods");

            migrationBuilder.DropTable(
                name: "VotingEpoches");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
