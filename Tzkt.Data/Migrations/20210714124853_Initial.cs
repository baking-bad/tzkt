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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KnownHead = table.Column<int>(type: "integer", nullable: false),
                    LastSync = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Protocol = table.Column<string>(type: "text", nullable: true),
                    NextProtocol = table.Column<string>(type: "text", nullable: true),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    VotingEpoch = table.Column<int>(type: "integer", nullable: false),
                    VotingPeriod = table.Column<int>(type: "integer", nullable: false),
                    AccountCounter = table.Column<int>(type: "integer", nullable: false),
                    OperationCounter = table.Column<int>(type: "integer", nullable: false),
                    ManagerCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapKeyCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapUpdateCounter = table.Column<int>(type: "integer", nullable: false),
                    StorageCounter = table.Column<int>(type: "integer", nullable: false),
                    ScriptCounter = table.Column<int>(type: "integer", nullable: false),
                    CommitmentsCount = table.Column<int>(type: "integer", nullable: false),
                    AccountsCount = table.Column<int>(type: "integer", nullable: false),
                    BlocksCount = table.Column<int>(type: "integer", nullable: false),
                    ProtocolsCount = table.Column<int>(type: "integer", nullable: false),
                    ActivationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    BallotOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DoubleBakingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DoubleEndorsingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    EndorsementOpsCount = table.Column<int>(type: "integer", nullable: false),
                    NonceRevelationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    OriginationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    ProposalOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RevealOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionOpsCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RevelationPenaltyOpsCount = table.Column<int>(type: "integer", nullable: false),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: false),
                    CyclesCount = table.Column<int>(type: "integer", nullable: false),
                    QuoteLevel = table.Column<int>(type: "integer", nullable: false),
                    QuoteBtc = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEur = table.Column<double>(type: "double precision", nullable: false),
                    QuoteUsd = table.Column<double>(type: "double precision", nullable: false),
                    QuoteCny = table.Column<double>(type: "double precision", nullable: false),
                    QuoteJpy = table.Column<double>(type: "double precision", nullable: false),
                    QuoteKrw = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEth = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakerCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Rolls = table.Column<int>(type: "integer", nullable: false),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: false),
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: false),
                    FutureBlocks = table.Column<int>(type: "integer", nullable: false),
                    OwnBlocks = table.Column<int>(type: "integer", nullable: false),
                    ExtraBlocks = table.Column<int>(type: "integer", nullable: false),
                    MissedOwnBlocks = table.Column<int>(type: "integer", nullable: false),
                    MissedExtraBlocks = table.Column<int>(type: "integer", nullable: false),
                    UncoveredOwnBlocks = table.Column<int>(type: "integer", nullable: false),
                    UncoveredExtraBlocks = table.Column<int>(type: "integer", nullable: false),
                    FutureEndorsements = table.Column<int>(type: "integer", nullable: false),
                    Endorsements = table.Column<int>(type: "integer", nullable: false),
                    MissedEndorsements = table.Column<int>(type: "integer", nullable: false),
                    UncoveredEndorsements = table.Column<int>(type: "integer", nullable: false),
                    FutureBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    OwnBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    ExtraBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedOwnBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedExtraBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    UncoveredOwnBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    UncoveredExtraBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    FutureEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    UncoveredEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    OwnBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    ExtraBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    MissedOwnBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    MissedExtraBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    UncoveredOwnBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    UncoveredExtraBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostDeposits = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostFees = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostDeposits = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostFees = table.Column<long>(type: "bigint", nullable: false),
                    RevelationRewards = table.Column<long>(type: "bigint", nullable: false),
                    RevelationLostRewards = table.Column<long>(type: "bigint", nullable: false),
                    RevelationLostFees = table.Column<long>(type: "bigint", nullable: false),
                    FutureBlockDeposits = table.Column<long>(type: "bigint", nullable: false),
                    BlockDeposits = table.Column<long>(type: "bigint", nullable: false),
                    FutureEndorsementDeposits = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementDeposits = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedBlocks = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedEndorsements = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakerCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakingRights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    Slots = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakingRights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BigMapKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigMapPtr = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Updates = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                    RawKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonKey = table.Column<string>(type: "jsonb", nullable: true),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMapKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BigMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ptr = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    KeyType = table.Column<byte[]>(type: "bytea", nullable: true),
                    ValueType = table.Column<byte[]>(type: "bytea", nullable: true),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TotalKeys = table.Column<int>(type: "integer", nullable: false),
                    ActiveKeys = table.Column<int>(type: "integer", nullable: false),
                    Updates = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMaps", x => x.Id);
                    table.UniqueConstraint("AK_BigMaps_Ptr", x => x.Ptr);
                });

            migrationBuilder.CreateTable(
                name: "BigMapUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigMapPtr = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true),
                    BigMapKeyId = table.Column<int>(type: "integer", nullable: true),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMapUpdates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commitments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character(37)", fixedLength: true, maxLength: 37, nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    SnapshotIndex = table.Column<int>(type: "integer", nullable: false),
                    SnapshotLevel = table.Column<int>(type: "integer", nullable: false),
                    TotalRolls = table.Column<int>(type: "integer", nullable: false),
                    TotalStaking = table.Column<long>(type: "bigint", nullable: false),
                    TotalDelegated = table.Column<long>(type: "bigint", nullable: false),
                    TotalDelegators = table.Column<int>(type: "integer", nullable: false),
                    TotalBakers = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    DelegatorId = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatorCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: true),
                    InitiatorId = table.Column<int>(type: "integer", nullable: false),
                    FirstPeriod = table.Column<int>(type: "integer", nullable: false),
                    LastPeriod = table.Column<int>(type: "integer", nullable: false),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Upvotes = table.Column<int>(type: "integer", nullable: false),
                    Rolls = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    FirstCycle = table.Column<int>(type: "integer", nullable: false),
                    FirstCycleLevel = table.Column<int>(type: "integer", nullable: false),
                    RampUpCycles = table.Column<int>(type: "integer", nullable: false),
                    NoRewardCycles = table.Column<int>(type: "integer", nullable: false),
                    PreservedCycles = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerCycle = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerCommitment = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerSnapshot = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerVoting = table.Column<int>(type: "integer", nullable: false),
                    TimeBetweenBlocks = table.Column<int>(type: "integer", nullable: false),
                    EndorsersPerBlock = table.Column<int>(type: "integer", nullable: false),
                    HardOperationGasLimit = table.Column<int>(type: "integer", nullable: false),
                    HardOperationStorageLimit = table.Column<int>(type: "integer", nullable: false),
                    HardBlockGasLimit = table.Column<int>(type: "integer", nullable: false),
                    TokensPerRoll = table.Column<long>(type: "bigint", nullable: false),
                    RevelationReward = table.Column<long>(type: "bigint", nullable: false),
                    BlockDeposit = table.Column<long>(type: "bigint", nullable: false),
                    BlockReward0 = table.Column<long>(type: "bigint", nullable: false),
                    BlockReward1 = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementDeposit = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementReward0 = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementReward1 = table.Column<long>(type: "bigint", nullable: false),
                    OriginationSize = table.Column<int>(type: "integer", nullable: false),
                    ByteCost = table.Column<int>(type: "integer", nullable: false),
                    ProposalQuorum = table.Column<int>(type: "integer", nullable: false),
                    BallotQuorumMin = table.Column<int>(type: "integer", nullable: false),
                    BallotQuorumMax = table.Column<int>(type: "integer", nullable: false),
                    LBSubsidy = table.Column<int>(type: "integer", nullable: false),
                    LBSunsetLevel = table.Column<int>(type: "integer", nullable: false),
                    LBEscapeThreshold = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                    table.UniqueConstraint("AK_Protocols_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Btc = table.Column<double>(type: "double precision", nullable: false),
                    Eur = table.Column<double>(type: "double precision", nullable: false),
                    Usd = table.Column<double>(type: "double precision", nullable: false),
                    Cny = table.Column<double>(type: "double precision", nullable: false),
                    Jpy = table.Column<double>(type: "double precision", nullable: false),
                    Krw = table.Column<double>(type: "double precision", nullable: false),
                    Eth = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    ParameterSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    StorageSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    CodeSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    TypeHash = table.Column<int>(type: "integer", nullable: false),
                    CodeHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Software",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlocksCount = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    ShortHash = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalBootstrapped = table.Column<long>(type: "bigint", nullable: false),
                    TotalCommitments = table.Column<long>(type: "bigint", nullable: false),
                    TotalActivated = table.Column<long>(type: "bigint", nullable: false),
                    TotalCreated = table.Column<long>(type: "bigint", nullable: false),
                    TotalBurned = table.Column<long>(type: "bigint", nullable: false),
                    TotalBanished = table.Column<long>(type: "bigint", nullable: false),
                    TotalVested = table.Column<long>(type: "bigint", nullable: false),
                    TotalFrozen = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Storages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VotingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalBakers = table.Column<int>(type: "integer", nullable: true),
                    TotalRolls = table.Column<int>(type: "integer", nullable: true),
                    UpvotesQuorum = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    TopUpvotes = table.Column<int>(type: "integer", nullable: true),
                    TopRolls = table.Column<int>(type: "integer", nullable: true),
                    ParticipationEma = table.Column<int>(type: "integer", nullable: true),
                    BallotsQuorum = table.Column<int>(type: "integer", nullable: true),
                    Supermajority = table.Column<int>(type: "integer", nullable: true),
                    YayBallots = table.Column<int>(type: "integer", nullable: true),
                    YayRolls = table.Column<int>(type: "integer", nullable: true),
                    NayBallots = table.Column<int>(type: "integer", nullable: true),
                    NayRolls = table.Column<int>(type: "integer", nullable: true),
                    PassBallots = table.Column<int>(type: "integer", nullable: true),
                    PassRolls = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingPeriods", x => x.Id);
                    table.UniqueConstraint("AK_VotingPeriods_Index", x => x.Index);
                });

            migrationBuilder.CreateTable(
                name: "VotingSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Rolls = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BallotOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    ProposalId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    Rolls = table.Column<int>(type: "integer", nullable: false),
                    Vote = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BallotOps_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProtoCode = table.Column<int>(type: "integer", nullable: false),
                    SoftwareId = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Validations = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<int>(type: "integer", nullable: false),
                    Operations = table.Column<int>(type: "integer", nullable: false),
                    Deposit = table.Column<long>(type: "bigint", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Fees = table.Column<long>(type: "bigint", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: true),
                    RevelationId = table.Column<int>(type: "integer", nullable: true),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    LBEscapeVote = table.Column<bool>(type: "boolean", nullable: false),
                    LBEscapeEma = table.Column<int>(type: "integer", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_Blocks_Software_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "Software",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character(36)", fixedLength: true, maxLength: 36, nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    ContractsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegationsCount = table.Column<int>(type: "integer", nullable: false),
                    OriginationsCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionsCount = table.Column<int>(type: "integer", nullable: false),
                    RevealsCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    DelegationLevel = table.Column<int>(type: "integer", nullable: true),
                    Staked = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Kind = table.Column<byte>(type: "smallint", nullable: true),
                    TypeHash = table.Column<int>(type: "integer", nullable: true),
                    CodeHash = table.Column<int>(type: "integer", nullable: true),
                    Tzips = table.Column<int>(type: "integer", nullable: true),
                    Spendable = table.Column<bool>(type: "boolean", nullable: true),
                    CreatorId = table.Column<int>(type: "integer", nullable: true),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    WeirdDelegateId = table.Column<int>(type: "integer", nullable: true),
                    Activated = table.Column<bool>(type: "boolean", nullable: true),
                    PublicKey = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    Revealed = table.Column<bool>(type: "boolean", nullable: true),
                    ActivationLevel = table.Column<int>(type: "integer", nullable: true),
                    DeactivationLevel = table.Column<int>(type: "integer", nullable: true),
                    FrozenDeposits = table.Column<long>(type: "bigint", nullable: true),
                    FrozenRewards = table.Column<long>(type: "bigint", nullable: true),
                    FrozenFees = table.Column<long>(type: "bigint", nullable: true),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: true),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: true),
                    BlocksCount = table.Column<int>(type: "integer", nullable: true),
                    EndorsementsCount = table.Column<int>(type: "integer", nullable: true),
                    BallotsCount = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleBakingCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleEndorsingCount = table.Column<int>(type: "integer", nullable: true),
                    NonceRevelationsCount = table.Column<int>(type: "integer", nullable: true),
                    RevelationPenaltiesCount = table.Column<int>(type: "integer", nullable: true),
                    SoftwareId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_DelegateId",
                        column: x => x.DelegateId,
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
                    table.ForeignKey(
                        name: "FK_Accounts_Blocks_FirstLevel",
                        column: x => x.FirstLevel,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accounts_Software_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "Software",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DelegationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    PrevDelegateId = table.Column<int>(type: "integer", nullable: true),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    Errors = table.Column<string>(type: "text", nullable: true),
                    InitiatorId = table.Column<int>(type: "integer", nullable: true),
                    Nonce = table.Column<int>(type: "integer", nullable: true)
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
                        name: "FK_DelegationOps_Accounts_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Accounts_PrevDelegateId",
                        column: x => x.PrevDelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DelegationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoubleBakingOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    AccuserReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    OffenderLostDeposit = table.Column<long>(type: "bigint", nullable: false),
                    OffenderLostReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderLostFee = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
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
                        name: "FK_DoubleBakingOps_Accounts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleBakingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoubleEndorsingOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    AccuserReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    OffenderLostDeposit = table.Column<long>(type: "bigint", nullable: false),
                    OffenderLostReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderLostFee = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
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
                        name: "FK_DoubleEndorsingOps_Accounts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoubleEndorsingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsementOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    Slots = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Deposit = table.Column<long>(type: "bigint", nullable: false),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
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
                name: "MigrationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    BalanceChange = table.Column<long>(type: "bigint", nullable: false),
                    ScriptId = table.Column<int>(type: "integer", nullable: true),
                    StorageId = table.Column<int>(type: "integer", nullable: true),
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MigrationOps_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MigrationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MigrationOps_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MigrationOps_Storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NonceRevelationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    RevealedLevel = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
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
                        name: "FK_NonceRevelationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
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
                name: "OriginationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    ScriptId = table.Column<int>(type: "integer", nullable: true),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    Errors = table.Column<string>(type: "text", nullable: true),
                    InitiatorId = table.Column<int>(type: "integer", nullable: true),
                    Nonce = table.Column<int>(type: "integer", nullable: true),
                    StorageId = table.Column<int>(type: "integer", nullable: true),
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true)
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
                        name: "FK_OriginationOps_Accounts_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OriginationOps_Storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProposalOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    ProposalId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    Rolls = table.Column<int>(type: "integer", nullable: false),
                    Duplicated = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalOps_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevealOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    table.PrimaryKey("PK_RevealOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevealOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevealOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevelationPenaltyOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    MissedLevel = table.Column<int>(type: "integer", nullable: false),
                    LostReward = table.Column<long>(type: "bigint", nullable: false),
                    LostFees = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevelationPenaltyOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevelationPenaltyOps_Accounts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RevelationPenaltyOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Entrypoint = table.Column<string>(type: "text", nullable: true),
                    RawParameters = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonParameters = table.Column<string>(type: "jsonb", nullable: true),
                    InternalOperations = table.Column<short>(type: "smallint", nullable: true),
                    InternalDelegations = table.Column<short>(type: "smallint", nullable: true),
                    InternalOriginations = table.Column<short>(type: "smallint", nullable: true),
                    InternalTransactions = table.Column<short>(type: "smallint", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    Errors = table.Column<string>(type: "text", nullable: true),
                    InitiatorId = table.Column<int>(type: "integer", nullable: true),
                    Nonce = table.Column<int>(type: "integer", nullable: true),
                    StorageId = table.Column<int>(type: "integer", nullable: true),
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Accounts_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Accounts",
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
                    table.ForeignKey(
                        name: "FK_TransactionOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionOps_Storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "AccountCounter", "AccountsCount", "ActivationOpsCount", "BallotOpsCount", "BigMapCounter", "BigMapKeyCounter", "BigMapUpdateCounter", "BlocksCount", "CommitmentsCount", "Cycle", "CyclesCount", "DelegationOpsCount", "DoubleBakingOpsCount", "DoubleEndorsingOpsCount", "EndorsementOpsCount", "Hash", "KnownHead", "LastSync", "Level", "ManagerCounter", "MigrationOpsCount", "NextProtocol", "NonceRevelationOpsCount", "OperationCounter", "OriginationOpsCount", "ProposalOpsCount", "ProposalsCount", "Protocol", "ProtocolsCount", "QuoteBtc", "QuoteCny", "QuoteEth", "QuoteEur", "QuoteJpy", "QuoteKrw", "QuoteLevel", "QuoteUsd", "RevealOpsCount", "RevelationPenaltyOpsCount", "ScriptCounter", "StorageCounter", "Timestamp", "TransactionOpsCount", "VotingEpoch", "VotingPeriod" },
                values: new object[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, "", 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), -1, 0, 0, "", 0, 0, 0, 0, 0, "", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, -1, 0.0, 0, 0, 0, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, -1, -1 });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Address",
                table: "Accounts",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CodeHash",
                table: "Accounts",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatorId",
                table: "Accounts",
                column: "CreatorId");

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
                name: "IX_Accounts_ManagerId",
                table: "Accounts",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SoftwareId",
                table: "Accounts",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Staked",
                table: "Accounts",
                column: "Staked");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type",
                table: "Accounts",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type_Kind",
                table: "Accounts",
                columns: new[] { "Type", "Kind" },
                filter: "\"Type\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type_Staked",
                table: "Accounts",
                columns: new[] { "Type", "Staked" },
                filter: "\"Type\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TypeHash",
                table: "Accounts",
                column: "TypeHash");

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
                name: "IX_BakerCycles_BakerId",
                table: "BakerCycles",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Cycle",
                table: "BakerCycles",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Cycle_BakerId",
                table: "BakerCycles",
                columns: new[] { "Cycle", "BakerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Id",
                table: "BakerCycles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_Cycle",
                table: "BakingRights",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_Cycle_BakerId",
                table: "BakingRights",
                columns: new[] { "Cycle", "BakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BakingRights_Level",
                table: "BakingRights",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_Epoch",
                table: "BallotOps",
                column: "Epoch");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_Level",
                table: "BallotOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_OpHash",
                table: "BallotOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_Period",
                table: "BallotOps",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_ProposalId",
                table: "BallotOps",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_BallotOps_SenderId",
                table: "BallotOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr",
                table: "BigMapKeys",
                column: "BigMapPtr");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr_Active",
                table: "BigMapKeys",
                columns: new[] { "BigMapPtr", "Active" },
                filter: "\"Active\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr_KeyHash",
                table: "BigMapKeys",
                columns: new[] { "BigMapPtr", "KeyHash" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_Id",
                table: "BigMapKeys",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_JsonKey",
                table: "BigMapKeys",
                column: "JsonKey")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_JsonValue",
                table: "BigMapKeys",
                column: "JsonValue")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_LastLevel",
                table: "BigMapKeys",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_ContractId",
                table: "BigMaps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_Id",
                table: "BigMaps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_Ptr",
                table: "BigMaps",
                column: "Ptr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapKeyId",
                table: "BigMapUpdates",
                column: "BigMapKeyId",
                filter: "\"BigMapKeyId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapPtr",
                table: "BigMapUpdates",
                column: "BigMapPtr");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_Id",
                table: "BigMapUpdates",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_Level",
                table: "BigMapUpdates",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_MigrationId",
                table: "BigMapUpdates",
                column: "MigrationId",
                filter: "\"MigrationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_OriginationId",
                table: "BigMapUpdates",
                column: "OriginationId",
                filter: "\"OriginationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_TransactionId",
                table: "BigMapUpdates",
                column: "TransactionId",
                filter: "\"TransactionId\" is not null");

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
                name: "IX_Blocks_SoftwareId",
                table: "Blocks",
                column: "SoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_Address",
                table: "Commitments",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_Id",
                table: "Commitments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_Index",
                table: "Cycles",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_DelegateId",
                table: "DelegationOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_InitiatorId",
                table: "DelegationOps",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_Level",
                table: "DelegationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_OpHash",
                table: "DelegationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_PrevDelegateId",
                table: "DelegationOps",
                column: "PrevDelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationOps_SenderId",
                table: "DelegationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle",
                table: "DelegatorCycles",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_BakerId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "BakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_DelegatorId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "DelegatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_DelegatorId",
                table: "DelegatorCycles",
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
                name: "IX_MigrationOps_AccountId",
                table: "MigrationOps",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_Level",
                table: "MigrationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_ScriptId",
                table: "MigrationOps",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_StorageId",
                table: "MigrationOps",
                column: "StorageId");

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
                name: "IX_OriginationOps_InitiatorId",
                table: "OriginationOps",
                column: "InitiatorId");

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
                name: "IX_OriginationOps_ScriptId",
                table: "OriginationOps",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_SenderId",
                table: "OriginationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_StorageId",
                table: "OriginationOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_Epoch",
                table: "ProposalOps",
                column: "Epoch");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_Level",
                table: "ProposalOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_OpHash",
                table: "ProposalOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_Period",
                table: "ProposalOps",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_ProposalId",
                table: "ProposalOps",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalOps_SenderId",
                table: "ProposalOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_Epoch",
                table: "Proposals",
                column: "Epoch");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_Hash",
                table: "Proposals",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Level",
                table: "Quotes",
                column: "Level",
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
                name: "IX_RevelationPenaltyOps_BakerId",
                table: "RevelationPenaltyOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_RevelationPenaltyOps_Level",
                table: "RevelationPenaltyOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_ContractId_Current",
                table: "Scripts",
                columns: new[] { "ContractId", "Current" },
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_Id",
                table: "Scripts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotBalances_Level",
                table: "SnapshotBalances",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Cycle",
                table: "Statistics",
                column: "Cycle",
                unique: true,
                filter: "\"Cycle\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Date",
                table: "Statistics",
                column: "Date",
                unique: true,
                filter: "\"Date\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Level",
                table: "Statistics",
                column: "Level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Storages_ContractId",
                table: "Storages",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_ContractId_Current",
                table: "Storages",
                columns: new[] { "ContractId", "Current" },
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Id",
                table: "Storages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Level",
                table: "Storages",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_InitiatorId",
                table: "TransactionOps",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_JsonParameters",
                table: "TransactionOps",
                column: "JsonParameters")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_Level",
                table: "TransactionOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_OpHash",
                table: "TransactionOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_SenderId",
                table: "TransactionOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_StorageId",
                table: "TransactionOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId",
                table: "TransactionOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_Epoch",
                table: "VotingPeriods",
                column: "Epoch");

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_Id",
                table: "VotingPeriods",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingPeriods_Index",
                table: "VotingPeriods",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_Period",
                table: "VotingSnapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_Period_BakerId",
                table: "VotingSnapshots",
                columns: new[] { "Period", "BakerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationOps_Accounts_AccountId",
                table: "ActivationOps",
                column: "AccountId",
                principalTable: "Accounts",
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
                name: "FK_BallotOps_Accounts_SenderId",
                table: "BallotOps",
                column: "SenderId",
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
                name: "BakerCycles");

            migrationBuilder.DropTable(
                name: "BakingRights");

            migrationBuilder.DropTable(
                name: "BallotOps");

            migrationBuilder.DropTable(
                name: "BigMapKeys");

            migrationBuilder.DropTable(
                name: "BigMaps");

            migrationBuilder.DropTable(
                name: "BigMapUpdates");

            migrationBuilder.DropTable(
                name: "Commitments");

            migrationBuilder.DropTable(
                name: "Cycles");

            migrationBuilder.DropTable(
                name: "DelegationOps");

            migrationBuilder.DropTable(
                name: "DelegatorCycles");

            migrationBuilder.DropTable(
                name: "DoubleBakingOps");

            migrationBuilder.DropTable(
                name: "DoubleEndorsingOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "MigrationOps");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "RevelationPenaltyOps");

            migrationBuilder.DropTable(
                name: "SnapshotBalances");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropTable(
                name: "TransactionOps");

            migrationBuilder.DropTable(
                name: "VotingPeriods");

            migrationBuilder.DropTable(
                name: "VotingSnapshots");

            migrationBuilder.DropTable(
                name: "Scripts");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Software");
        }
    }
}
