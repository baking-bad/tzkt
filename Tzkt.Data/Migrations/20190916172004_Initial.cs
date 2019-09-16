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
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    AddressId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cycles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<int>(nullable: false),
                    Snapshot = table.Column<int>(nullable: false),
                    ActiveBakers = table.Column<int>(nullable: false),
                    ActiveDelegators = table.Column<int>(nullable: false),
                    TotalRolls = table.Column<int>(nullable: false),
                    TotalBalances = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Weight = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
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
                name: "BakerCycles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    StakingBalance = table.Column<long>(nullable: false),
                    Blocks = table.Column<int>(nullable: false),
                    BlocksMissed = table.Column<int>(nullable: false),
                    BlocksExtra = table.Column<int>(nullable: false),
                    Endorsements = table.Column<int>(nullable: false),
                    EndorsementsMissed = table.Column<int>(nullable: false),
                    BlocksReward = table.Column<long>(nullable: false),
                    EndorsementsReward = table.Column<long>(nullable: false),
                    FeesReward = table.Column<long>(nullable: false),
                    AccusationReward = table.Column<long>(nullable: false),
                    AccusationLostDeposit = table.Column<long>(nullable: false),
                    AccusationLostReward = table.Column<long>(nullable: false),
                    AccusationLostFee = table.Column<long>(nullable: false),
                    RevelationReward = table.Column<long>(nullable: false),
                    RevelationLostReward = table.Column<long>(nullable: false),
                    RevelationLostFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakerCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakingRights",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakingRights", x => x.Id);
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
                    ProtocolId = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: true),
                    Priority = table.Column<int>(nullable: false),
                    Operations = table.Column<int>(nullable: false),
                    OperationsMask = table.Column<int>(nullable: false),
                    Validations = table.Column<int>(nullable: false),
                    RevelationId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.UniqueConstraint("AK_Blocks_Level", x => x.Level);
                    table.ForeignKey(
                        name: "FK_Blocks_Protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(nullable: false),
                    Address = table.Column<string>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true),
                    Staked = table.Column<bool>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    Operations = table.Column<int>(nullable: false),
                    PublicKey = table.Column<string>(maxLength: 65, nullable: true),
                    Counter = table.Column<long>(nullable: true),
                    ActivationLevel = table.Column<int>(nullable: true),
                    DeactivationLevel = table.Column<int>(nullable: true),
                    FrozenDeposits = table.Column<long>(nullable: true),
                    FrozenRewards = table.Column<long>(nullable: true),
                    FrozenFees = table.Column<long>(nullable: true),
                    Delegators = table.Column<int>(nullable: true),
                    StakingBalance = table.Column<long>(nullable: true),
                    ManagerId = table.Column<int>(nullable: true),
                    OriginatorId = table.Column<int>(nullable: true),
                    Delegatable = table.Column<bool>(nullable: true),
                    Spendable = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.UniqueConstraint("AK_Addresses_Address", x => x.Address);
                    table.ForeignKey(
                        name: "FK_Addresses_Addresses_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Addresses_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Addresses_OriginatorId",
                        column: x => x.OriginatorId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Blocks_ActivationLevel",
                        column: x => x.ActivationLevel,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Addresses_Blocks_DeactivationLevel",
                        column: x => x.DeactivationLevel,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DelegatorSnapshots",
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
                    table.PrimaryKey("PK_DelegatorSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegatorSnapshots_Addresses_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DelegatorSnapshots_Addresses_DelegatorId",
                        column: x => x.DelegatorId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    OffenderLoss = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleBakingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Addresses_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Addresses_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Addresses",
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
                    OffenderLoss = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleEndorsingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Addresses_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Addresses_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Addresses",
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
                    Reward = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsementOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndorsementOps_Addresses_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Addresses",
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
                name: "EndorsingRights",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(nullable: false),
                    BakerId = table.Column<int>(nullable: false),
                    Slots = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsingRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndorsingRights_Addresses_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
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
                    RevelationLevel = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonceRevelationOps", x => x.Id);
                    table.UniqueConstraint("AK_NonceRevelationOps_RevelationLevel", x => x.RevelationLevel);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Addresses_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonceRevelationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
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
                        name: "FK_Proposals_Addresses_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Addresses",
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
                    Fee = table.Column<long>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    TargetId = table.Column<int>(nullable: false),
                    TargetAllocated = table.Column<bool>(nullable: false),
                    Amount = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: false)
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
                        name: "FK_TransactionOps_Addresses_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Addresses_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_ProposalOps_Addresses_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Addresses",
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
                    Fee = table.Column<long>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    DelegateId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Addresses_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Addresses",
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
                        name: "FK_DelegationOps_Addresses_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Addresses",
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
                    Fee = table.Column<long>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    ContractId = table.Column<int>(nullable: false),
                    DelegateId = table.Column<int>(nullable: true),
                    ManagerId = table.Column<int>(nullable: false),
                    Delegatable = table.Column<bool>(nullable: false),
                    Spendable = table.Column<bool>(nullable: false),
                    Balance = table.Column<long>(nullable: false),
                    StorageFee = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Addresses_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Addresses_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Addresses_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_TransactionOps_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TransactionOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Addresses_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Fee = table.Column<long>(nullable: false),
                    Status = table.Column<byte>(nullable: false),
                    ParentId = table.Column<int>(nullable: true),
                    Nonce = table.Column<int>(nullable: true),
                    PublicKey = table.Column<string>(maxLength: 65, nullable: false)
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
                        name: "FK_RevealOps_TransactionOps_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TransactionOps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevealOps_Addresses_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "Hash", "Level", "Protocol", "Synced", "Timestamp" },
                values: new object[] { -1, "", -1, "", false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

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
                name: "IX_Addresses_Address",
                table: "Addresses",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_DelegateId",
                table: "Addresses",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_Id",
                table: "Addresses",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_Staked",
                table: "Addresses",
                column: "Staked");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ManagerId",
                table: "Addresses",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_OriginatorId",
                table: "Addresses",
                column: "OriginatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ActivationLevel",
                table: "Addresses",
                column: "ActivationLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_DeactivationLevel",
                table: "Addresses",
                column: "DeactivationLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_BakerId",
                table: "BakerCycles",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_BakerId",
                table: "BakingRights",
                column: "BakerId");

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
                name: "IX_Blocks_ProtocolId",
                table: "Blocks",
                column: "ProtocolId");

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
                name: "IX_DelegatorSnapshots_BakerId",
                table: "DelegatorSnapshots",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorSnapshots_DelegatorId",
                table: "DelegatorSnapshots",
                column: "DelegatorId");

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
                name: "IX_EndorsingRights_BakerId",
                table: "EndorsingRights",
                column: "BakerId");

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
                name: "IX_NonceRevelationOps_RevelationLevel",
                table: "NonceRevelationOps",
                column: "RevelationLevel");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ContractId",
                table: "OriginationOps",
                column: "ContractId",
                unique: true);

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
                name: "IX_Proposals_Id",
                table: "Proposals",
                column: "Id",
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
                name: "IX_RevealOps_ParentId",
                table: "RevealOps",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RevealOps_SenderId",
                table: "RevealOps",
                column: "SenderId");

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
                name: "IX_VotingEpoches_Id",
                table: "VotingEpoches",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_EpochId",
                table: "VotingPeriods",
                column: "EpochId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_Id",
                table: "VotingPeriods",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationOps_Addresses_AccountId",
                table: "ActivationOps",
                column: "AccountId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationOps_Blocks_Level",
                table: "ActivationOps",
                column: "Level",
                principalTable: "Blocks",
                principalColumn: "Level",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BakerCycles_Addresses_BakerId",
                table: "BakerCycles",
                column: "BakerId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BakingRights_Addresses_BakerId",
                table: "BakingRights",
                column: "BakerId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BallotOps_Addresses_SenderId",
                table: "BallotOps",
                column: "SenderId",
                principalTable: "Addresses",
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
                name: "FK_BallotOps_Proposals_ProposalId",
                table: "BallotOps",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Addresses_BakerId",
                table: "Blocks",
                column: "BakerId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_NonceRevelationOps_RevelationId",
                table: "Blocks",
                column: "RevelationId",
                principalTable: "NonceRevelationOps",
                principalColumn: "RevelationLevel",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Addresses_BakerId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_NonceRevelationOps_Addresses_BakerId",
                table: "NonceRevelationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_NonceRevelationOps_Blocks_Level",
                table: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "ActivationOps");

            migrationBuilder.DropTable(
                name: "AppState");

            migrationBuilder.DropTable(
                name: "BakerCycles");

            migrationBuilder.DropTable(
                name: "BakingRights");

            migrationBuilder.DropTable(
                name: "BalanceSnapshots");

            migrationBuilder.DropTable(
                name: "BallotOps");

            migrationBuilder.DropTable(
                name: "Cycles");

            migrationBuilder.DropTable(
                name: "DelegationOps");

            migrationBuilder.DropTable(
                name: "DelegatorSnapshots");

            migrationBuilder.DropTable(
                name: "DoubleBakingOps");

            migrationBuilder.DropTable(
                name: "DoubleEndorsingOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "EndorsingRights");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "TransactionOps");

            migrationBuilder.DropTable(
                name: "VotingPeriods");

            migrationBuilder.DropTable(
                name: "VotingEpoches");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "NonceRevelationOps");
        }
    }
}
