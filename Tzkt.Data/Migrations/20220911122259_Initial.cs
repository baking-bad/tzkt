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
                    Chain = table.Column<string>(type: "text", nullable: true),
                    ChainId = table.Column<string>(type: "text", nullable: true),
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
                    OperationCounter = table.Column<long>(type: "bigint", nullable: false),
                    ManagerCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapKeyCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapUpdateCounter = table.Column<int>(type: "integer", nullable: false),
                    StorageCounter = table.Column<int>(type: "integer", nullable: false),
                    ScriptCounter = table.Column<int>(type: "integer", nullable: false),
                    EventCounter = table.Column<int>(type: "integer", nullable: false),
                    CommitmentsCount = table.Column<int>(type: "integer", nullable: false),
                    AccountsCount = table.Column<int>(type: "integer", nullable: false),
                    BlocksCount = table.Column<int>(type: "integer", nullable: false),
                    ProtocolsCount = table.Column<int>(type: "integer", nullable: false),
                    ActivationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    BallotOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DoubleBakingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DoubleEndorsingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DoublePreendorsingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    EndorsementOpsCount = table.Column<int>(type: "integer", nullable: false),
                    PreendorsementOpsCount = table.Column<int>(type: "integer", nullable: false),
                    NonceRevelationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    VdfRevelationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    OriginationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    ProposalOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RevealOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RegisterConstantOpsCount = table.Column<int>(type: "integer", nullable: false),
                    EndorsingRewardOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SetDepositsLimitOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupOriginationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupSubmitBatchOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupCommitOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupFinalizeCommitmentOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupRemoveCommitmentOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupReturnBondOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupRejectionOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupDispatchTicketsOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TransferTicketOpsCount = table.Column<int>(type: "integer", nullable: false),
                    IncreasePaidStorageOpsCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RevelationPenaltyOpsCount = table.Column<int>(type: "integer", nullable: false),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: false),
                    CyclesCount = table.Column<int>(type: "integer", nullable: false),
                    ConstantsCount = table.Column<int>(type: "integer", nullable: false),
                    TokensCount = table.Column<int>(type: "integer", nullable: false),
                    TokenBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TokenTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    EventsCount = table.Column<int>(type: "integer", nullable: false),
                    QuoteLevel = table.Column<int>(type: "integer", nullable: false),
                    QuoteBtc = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEur = table.Column<double>(type: "double precision", nullable: false),
                    QuoteUsd = table.Column<double>(type: "double precision", nullable: false),
                    QuoteCny = table.Column<double>(type: "double precision", nullable: false),
                    QuoteJpy = table.Column<double>(type: "double precision", nullable: false),
                    QuoteKrw = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEth = table.Column<double>(type: "double precision", nullable: false),
                    QuoteGbp = table.Column<double>(type: "double precision", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
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
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: false),
                    ActiveStake = table.Column<long>(type: "bigint", nullable: false),
                    SelectedStake = table.Column<long>(type: "bigint", nullable: false),
                    FutureBlocks = table.Column<int>(type: "integer", nullable: false),
                    Blocks = table.Column<int>(type: "integer", nullable: false),
                    MissedBlocks = table.Column<int>(type: "integer", nullable: false),
                    FutureEndorsements = table.Column<int>(type: "integer", nullable: false),
                    Endorsements = table.Column<int>(type: "integer", nullable: false),
                    MissedEndorsements = table.Column<int>(type: "integer", nullable: false),
                    FutureBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    BlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    FutureEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    BlockFees = table.Column<long>(type: "bigint", nullable: false),
                    MissedBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLosses = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLosses = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingLosses = table.Column<long>(type: "bigint", nullable: false),
                    RevelationRewards = table.Column<long>(type: "bigint", nullable: false),
                    RevelationLosses = table.Column<long>(type: "bigint", nullable: false),
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
                    Round = table.Column<int>(type: "integer", nullable: true),
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
                    OriginationId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    MigrationId = table.Column<long>(type: "bigint", nullable: true),
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
                    TotalStaking = table.Column<long>(type: "bigint", nullable: false),
                    TotalDelegated = table.Column<long>(type: "bigint", nullable: false),
                    TotalDelegators = table.Column<int>(type: "integer", nullable: false),
                    TotalBakers = table.Column<int>(type: "integer", nullable: false),
                    SelectedBakers = table.Column<int>(type: "integer", nullable: false),
                    SelectedStake = table.Column<long>(type: "bigint", nullable: false),
                    Seed = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false)
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
                name: "EndorsingRewardOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Expected = table.Column<long>(type: "bigint", nullable: false),
                    Received = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsingRewardOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    ContractCodeHash = table.Column<int>(type: "integer", nullable: false),
                    TransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<byte[]>(type: "bytea", nullable: true),
                    RawPayload = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonPayload = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreezerUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Change = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezerUpdates", x => x.Id);
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
                    VotingPower = table.Column<long>(type: "bigint", nullable: false),
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
                    LBToggleThreshold = table.Column<int>(type: "integer", nullable: false),
                    ConsensusThreshold = table.Column<int>(type: "integer", nullable: false),
                    MinParticipationNumerator = table.Column<int>(type: "integer", nullable: false),
                    MinParticipationDenominator = table.Column<int>(type: "integer", nullable: false),
                    MaxSlashingPeriod = table.Column<int>(type: "integer", nullable: false),
                    FrozenDepositsPercentage = table.Column<int>(type: "integer", nullable: false),
                    DoubleBakingPunishment = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingPunishmentNumerator = table.Column<int>(type: "integer", nullable: false),
                    DoubleEndorsingPunishmentDenominator = table.Column<int>(type: "integer", nullable: false),
                    MaxBakingReward = table.Column<long>(type: "bigint", nullable: false),
                    MaxEndorsingReward = table.Column<long>(type: "bigint", nullable: false),
                    TxRollupOriginationSize = table.Column<int>(type: "integer", nullable: false),
                    TxRollupCommitmentBond = table.Column<long>(type: "bigint", nullable: false),
                    Dictator = table.Column<string>(type: "text", nullable: true),
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
                    Eth = table.Column<double>(type: "double precision", nullable: false),
                    Gbp = table.Column<double>(type: "double precision", nullable: false)
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
                    OriginationId = table.Column<long>(type: "bigint", nullable: true),
                    MigrationId = table.Column<long>(type: "bigint", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    ParameterSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    StorageSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    CodeSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    Views = table.Column<byte[][]>(type: "bytea[]", nullable: true),
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
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: true),
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: true),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: true)
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
                    TotalFrozen = table.Column<long>(type: "bigint", nullable: false),
                    TotalRollupBonds = table.Column<long>(type: "bigint", nullable: false)
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
                    OriginationId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    MigrationId = table.Column<long>(type: "bigint", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<string>(type: "text", nullable: false),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<int>(type: "integer", nullable: false),
                    FirstMinterId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    BalancesCount = table.Column<int>(type: "integer", nullable: false),
                    HoldersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMinted = table.Column<string>(type: "text", nullable: false),
                    TotalBurned = table.Column<string>(type: "text", nullable: false),
                    TotalSupply = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenTransfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    TokenId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    OriginationId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    MigrationId = table.Column<long>(type: "bigint", nullable: true),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransfers", x => x.Id);
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
                    Dictator = table.Column<int>(type: "integer", nullable: false),
                    TotalBakers = table.Column<int>(type: "integer", nullable: true),
                    TotalVotingPower = table.Column<long>(type: "bigint", nullable: true),
                    UpvotesQuorum = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    TopUpvotes = table.Column<int>(type: "integer", nullable: true),
                    TopVotingPower = table.Column<long>(type: "bigint", nullable: true),
                    ParticipationEma = table.Column<int>(type: "integer", nullable: true),
                    BallotsQuorum = table.Column<int>(type: "integer", nullable: true),
                    Supermajority = table.Column<int>(type: "integer", nullable: true),
                    YayBallots = table.Column<int>(type: "integer", nullable: true),
                    NayBallots = table.Column<int>(type: "integer", nullable: true),
                    PassBallots = table.Column<int>(type: "integer", nullable: true),
                    YayVotingPower = table.Column<long>(type: "bigint", nullable: true),
                    NayVotingPower = table.Column<long>(type: "bigint", nullable: true),
                    PassVotingPower = table.Column<long>(type: "bigint", nullable: true)
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
                    VotingPower = table.Column<long>(type: "bigint", nullable: false),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    ProposalId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    VotingPower = table.Column<long>(type: "bigint", nullable: false),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProtoCode = table.Column<int>(type: "integer", nullable: false),
                    SoftwareId = table.Column<int>(type: "integer", nullable: true),
                    PayloadRound = table.Column<int>(type: "integer", nullable: false),
                    BlockRound = table.Column<int>(type: "integer", nullable: false),
                    Validations = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<int>(type: "integer", nullable: false),
                    Operations = table.Column<int>(type: "integer", nullable: false),
                    Deposit = table.Column<long>(type: "bigint", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Bonus = table.Column<long>(type: "bigint", nullable: false),
                    Fees = table.Column<long>(type: "bigint", nullable: false),
                    ProposerId = table.Column<int>(type: "integer", nullable: true),
                    ProducerId = table.Column<int>(type: "integer", nullable: true),
                    RevelationId = table.Column<long>(type: "bigint", nullable: true),
                    ResetBakerDeactivation = table.Column<int>(type: "integer", nullable: true),
                    ResetProposerDeactivation = table.Column<int>(type: "integer", nullable: true),
                    LBToggle = table.Column<bool>(type: "boolean", nullable: true),
                    LBToggleEma = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
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
                    Address = table.Column<string>(type: "character varying(37)", maxLength: 37, nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    RollupBonds = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    ContractsCount = table.Column<int>(type: "integer", nullable: false),
                    RollupsCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveTokensCount = table.Column<int>(type: "integer", nullable: false),
                    TokenBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TokenTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    DelegationsCount = table.Column<int>(type: "integer", nullable: false),
                    OriginationsCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionsCount = table.Column<int>(type: "integer", nullable: false),
                    RevealsCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupOriginationCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupSubmitBatchCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupCommitCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupReturnBondCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupFinalizeCommitmentCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupRemoveCommitmentCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupRejectionCount = table.Column<int>(type: "integer", nullable: false),
                    TxRollupDispatchTicketsCount = table.Column<int>(type: "integer", nullable: false),
                    TransferTicketCount = table.Column<int>(type: "integer", nullable: false),
                    IncreasePaidStorageCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    DelegationLevel = table.Column<int>(type: "integer", nullable: true),
                    Staked = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Kind = table.Column<byte>(type: "smallint", nullable: true),
                    TypeHash = table.Column<int>(type: "integer", nullable: true),
                    CodeHash = table.Column<int>(type: "integer", nullable: true),
                    Tags = table.Column<int>(type: "integer", nullable: true),
                    TokensCount = table.Column<int>(type: "integer", nullable: true),
                    EventsCount = table.Column<int>(type: "integer", nullable: true),
                    Spendable = table.Column<bool>(type: "boolean", nullable: true),
                    CreatorId = table.Column<int>(type: "integer", nullable: true),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    WeirdDelegateId = table.Column<int>(type: "integer", nullable: true),
                    Activated = table.Column<bool>(type: "boolean", nullable: true),
                    PublicKey = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    Revealed = table.Column<bool>(type: "boolean", nullable: true),
                    RegisterConstantsCount = table.Column<int>(type: "integer", nullable: true),
                    SetDepositsLimitsCount = table.Column<int>(type: "integer", nullable: true),
                    ActivationLevel = table.Column<int>(type: "integer", nullable: true),
                    DeactivationLevel = table.Column<int>(type: "integer", nullable: true),
                    FrozenDepositLimit = table.Column<long>(type: "bigint", nullable: true),
                    FrozenDeposit = table.Column<long>(type: "bigint", nullable: true),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: true),
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: true),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: true),
                    BlocksCount = table.Column<int>(type: "integer", nullable: true),
                    EndorsementsCount = table.Column<int>(type: "integer", nullable: true),
                    PreendorsementsCount = table.Column<int>(type: "integer", nullable: true),
                    BallotsCount = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleBakingCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleEndorsingCount = table.Column<int>(type: "integer", nullable: true),
                    DoublePreendorsingCount = table.Column<int>(type: "integer", nullable: true),
                    NonceRevelationsCount = table.Column<int>(type: "integer", nullable: true),
                    VdfRevelationsCount = table.Column<int>(type: "integer", nullable: true),
                    RevelationPenaltiesCount = table.Column<int>(type: "integer", nullable: true),
                    EndorsingRewardsCount = table.Column<int>(type: "integer", nullable: true),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderCodeHash = table.Column<int>(type: "integer", nullable: true),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    AccuserReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    OffenderLoss = table.Column<long>(type: "bigint", nullable: false),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    AccuserReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    OffenderLoss = table.Column<long>(type: "bigint", nullable: false),
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
                name: "DoublePreendorsingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    AccuserReward = table.Column<long>(type: "bigint", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    OffenderLoss = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoublePreendorsingOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoublePreendorsingOps_Accounts_AccuserId",
                        column: x => x.AccuserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoublePreendorsingOps_Accounts_OffenderId",
                        column: x => x.OffenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoublePreendorsingOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndorsementOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
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
                name: "IncreasePaidStorageOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_IncreasePaidStorageOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncreasePaidStorageOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncreasePaidStorageOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MigrationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    BalanceChange = table.Column<long>(type: "bigint", nullable: false),
                    ScriptId = table.Column<int>(type: "integer", nullable: true),
                    StorageId = table.Column<int>(type: "integer", nullable: true),
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true),
                    TokenTransfers = table.Column<int>(type: "integer", nullable: true),
                    SubIds = table.Column<int>(type: "integer", nullable: true)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    RevealedLevel = table.Column<int>(type: "integer", nullable: false),
                    RevealedCycle = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderCodeHash = table.Column<int>(type: "integer", nullable: true),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    ContractCodeHash = table.Column<int>(type: "integer", nullable: true),
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
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true),
                    TokenTransfers = table.Column<int>(type: "integer", nullable: true),
                    SubIds = table.Column<int>(type: "integer", nullable: true)
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
                name: "PreendorsementOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    Slots = table.Column<int>(type: "integer", nullable: false),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreendorsementOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreendorsementOps_Accounts_DelegateId",
                        column: x => x.DelegateId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreendorsementOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposalOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    ProposalId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    VotingPower = table.Column<long>(type: "bigint", nullable: false),
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
                name: "RegisterConstantOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                    Value = table.Column<byte[]>(type: "bytea", nullable: true),
                    Refs = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_RegisterConstantOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegisterConstantOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegisterConstantOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevealOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    MissedLevel = table.Column<int>(type: "integer", nullable: false),
                    Loss = table.Column<long>(type: "bigint", nullable: false)
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
                name: "SetDepositsLimitOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Limit = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_SetDepositsLimitOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetDepositsLimitOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SetDepositsLimitOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderCodeHash = table.Column<int>(type: "integer", nullable: true),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    TargetCodeHash = table.Column<int>(type: "integer", nullable: true),
                    ResetDeactivation = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Entrypoint = table.Column<string>(type: "text", nullable: true),
                    RawParameters = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonParameters = table.Column<string>(type: "jsonb", nullable: true),
                    InternalOperations = table.Column<short>(type: "smallint", nullable: true),
                    InternalDelegations = table.Column<short>(type: "smallint", nullable: true),
                    InternalOriginations = table.Column<short>(type: "smallint", nullable: true),
                    InternalTransactions = table.Column<short>(type: "smallint", nullable: true),
                    EventsCount = table.Column<int>(type: "integer", nullable: true),
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
                    BigMapUpdates = table.Column<int>(type: "integer", nullable: true),
                    TokenTransfers = table.Column<int>(type: "integer", nullable: true),
                    SubIds = table.Column<int>(type: "integer", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "TransferTicketOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    TicketerId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    RawType = table.Column<byte[]>(type: "bytea", nullable: true),
                    RawContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonContent = table.Column<string>(type: "text", nullable: true),
                    Entrypoint = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_TransferTicketOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferTicketOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferTicketOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupCommitOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
                    Bond = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_TxRollupCommitOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupCommitOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupCommitOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupDispatchTicketsOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupDispatchTicketsOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupDispatchTicketsOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupDispatchTicketsOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupFinalizeCommitmentOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupFinalizeCommitmentOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupFinalizeCommitmentOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupFinalizeCommitmentOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupOriginationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupOriginationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupOriginationOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupOriginationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupRejectionOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
                    CommitterId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Loss = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_TxRollupRejectionOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupRejectionOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupRejectionOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupRemoveCommitmentOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupRemoveCommitmentOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupRemoveCommitmentOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupRemoveCommitmentOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupReturnBondOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
                    Bond = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_TxRollupReturnBondOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupReturnBondOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupReturnBondOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupSubmitBatchOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupSubmitBatchOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TxRollupSubmitBatchOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TxRollupSubmitBatchOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VdfRevelationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Solution = table.Column<byte[]>(type: "bytea", nullable: true),
                    Proof = table.Column<byte[]>(type: "bytea", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VdfRevelationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VdfRevelationOps_Accounts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VdfRevelationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "AccountCounter", "AccountsCount", "ActivationOpsCount", "BallotOpsCount", "BigMapCounter", "BigMapKeyCounter", "BigMapUpdateCounter", "BlocksCount", "Chain", "ChainId", "CommitmentsCount", "ConstantsCount", "Cycle", "CyclesCount", "DelegationOpsCount", "DoubleBakingOpsCount", "DoubleEndorsingOpsCount", "DoublePreendorsingOpsCount", "EndorsementOpsCount", "EndorsingRewardOpsCount", "EventCounter", "EventsCount", "Hash", "IncreasePaidStorageOpsCount", "KnownHead", "LastSync", "Level", "ManagerCounter", "Metadata", "MigrationOpsCount", "NextProtocol", "NonceRevelationOpsCount", "OperationCounter", "OriginationOpsCount", "PreendorsementOpsCount", "ProposalOpsCount", "ProposalsCount", "Protocol", "ProtocolsCount", "QuoteBtc", "QuoteCny", "QuoteEth", "QuoteEur", "QuoteGbp", "QuoteJpy", "QuoteKrw", "QuoteLevel", "QuoteUsd", "RegisterConstantOpsCount", "RevealOpsCount", "RevelationPenaltyOpsCount", "ScriptCounter", "SetDepositsLimitOpsCount", "StorageCounter", "Timestamp", "TokenBalancesCount", "TokenTransfersCount", "TokensCount", "TransactionOpsCount", "TransferTicketOpsCount", "TxRollupCommitOpsCount", "TxRollupDispatchTicketsOpsCount", "TxRollupFinalizeCommitmentOpsCount", "TxRollupOriginationOpsCount", "TxRollupRejectionOpsCount", "TxRollupRemoveCommitmentOpsCount", "TxRollupReturnBondOpsCount", "TxRollupSubmitBatchOpsCount", "VdfRevelationOpsCount", "VotingEpoch", "VotingPeriod" },
                values: new object[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, null, null, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, "", 0, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), -1, 0, null, 0, "", 0, 0L, 0, 0, 0, 0, "", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, -1, 0.0, 0, 0, 0, 0, 0, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1 });

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
                name: "IX_Blocks_ProducerId",
                table: "Blocks",
                column: "ProducerId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ProposerId",
                table: "Blocks",
                column: "ProposerId");

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
                name: "IX_DelegationOps_SenderCodeHash",
                table: "DelegationOps",
                column: "SenderCodeHash",
                filter: "\"SenderCodeHash\" IS NOT NULL");

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
                name: "IX_DoublePreendorsingOps_AccuserId",
                table: "DoublePreendorsingOps",
                column: "AccuserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoublePreendorsingOps_Level",
                table: "DoublePreendorsingOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DoublePreendorsingOps_OffenderId",
                table: "DoublePreendorsingOps",
                column: "OffenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DoublePreendorsingOps_OpHash",
                table: "DoublePreendorsingOps",
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
                name: "IX_EndorsingRewardOps_BakerId",
                table: "EndorsingRewardOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_EndorsingRewardOps_Level",
                table: "EndorsingRewardOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractCodeHash",
                table: "Events",
                column: "ContractCodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractCodeHash_Tag",
                table: "Events",
                columns: new[] { "ContractCodeHash", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractId",
                table: "Events",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractId_Tag",
                table: "Events",
                columns: new[] { "ContractId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Id",
                table: "Events",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_JsonPayload",
                table: "Events",
                column: "JsonPayload")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Level",
                table: "Events",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Tag",
                table: "Events",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_Events_TransactionId",
                table: "Events",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FreezerUpdates_Cycle",
                table: "FreezerUpdates",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_ContractId",
                table: "IncreasePaidStorageOps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_Level",
                table: "IncreasePaidStorageOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_OpHash",
                table: "IncreasePaidStorageOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_SenderId",
                table: "IncreasePaidStorageOps",
                column: "SenderId");

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
                name: "IX_NonceRevelationOps_RevealedCycle",
                table: "NonceRevelationOps",
                column: "RevealedCycle");

            migrationBuilder.CreateIndex(
                name: "IX_NonceRevelationOps_SenderId",
                table: "NonceRevelationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ContractCodeHash",
                table: "OriginationOps",
                column: "ContractCodeHash",
                filter: "\"ContractCodeHash\" IS NOT NULL");

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
                name: "IX_OriginationOps_SenderCodeHash",
                table: "OriginationOps",
                column: "SenderCodeHash",
                filter: "\"SenderCodeHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_SenderId",
                table: "OriginationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_StorageId",
                table: "OriginationOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_PreendorsementOps_DelegateId",
                table: "PreendorsementOps",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_PreendorsementOps_Level",
                table: "PreendorsementOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_PreendorsementOps_OpHash",
                table: "PreendorsementOps",
                column: "OpHash");

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
                name: "IX_RegisterConstantOps_Address",
                table: "RegisterConstantOps",
                column: "Address",
                unique: true,
                filter: "\"Address\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_Level",
                table: "RegisterConstantOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_OpHash",
                table: "RegisterConstantOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_SenderId",
                table: "RegisterConstantOps",
                column: "SenderId");

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
                name: "IX_SetDepositsLimitOps_Level",
                table: "SetDepositsLimitOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SetDepositsLimitOps_OpHash",
                table: "SetDepositsLimitOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SetDepositsLimitOps_SenderId",
                table: "SetDepositsLimitOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotBalances_Level",
                table: "SnapshotBalances",
                column: "Level",
                filter: "\"DelegateId\" IS NULL");

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
                name: "IX_TokenBalances_AccountId",
                table: "TokenBalances",
                column: "AccountId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_ContractId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_ContractId",
                table: "TokenBalances",
                column: "ContractId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_Id",
                table: "TokenBalances",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_IndexedAt",
                table: "TokenBalances",
                column: "IndexedAt",
                filter: "\"IndexedAt\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_LastLevel",
                table: "TokenBalances",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_TokenId",
                table: "TokenBalances",
                column: "TokenId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ContractId",
                table: "Tokens",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_ContractId_TokenId",
                table: "Tokens",
                columns: new[] { "ContractId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_FirstMinterId",
                table: "Tokens",
                column: "FirstMinterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Id",
                table: "Tokens",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_IndexedAt",
                table: "Tokens",
                column: "IndexedAt",
                filter: "\"IndexedAt\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_LastLevel",
                table: "Tokens",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Metadata",
                table: "Tokens",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ContractId",
                table: "TokenTransfers",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_FromId",
                table: "TokenTransfers",
                column: "FromId",
                filter: "\"FromId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_Id",
                table: "TokenTransfers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_IndexedAt",
                table: "TokenTransfers",
                column: "IndexedAt",
                filter: "\"IndexedAt\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_Level",
                table: "TokenTransfers",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_MigrationId",
                table: "TokenTransfers",
                column: "MigrationId",
                filter: "\"MigrationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_OriginationId",
                table: "TokenTransfers",
                column: "OriginationId",
                filter: "\"OriginationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ToId",
                table: "TokenTransfers",
                column: "ToId",
                filter: "\"ToId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TokenId",
                table: "TokenTransfers",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TransactionId",
                table: "TokenTransfers",
                column: "TransactionId",
                filter: "\"TransactionId\" is not null");

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
                name: "IX_TransactionOps_SenderCodeHash",
                table: "TransactionOps",
                column: "SenderCodeHash",
                filter: "\"SenderCodeHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_SenderId",
                table: "TransactionOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_StorageId",
                table: "TransactionOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetCodeHash",
                table: "TransactionOps",
                column: "TargetCodeHash",
                filter: "\"TargetCodeHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId",
                table: "TransactionOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTicketOps_Level",
                table: "TransferTicketOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTicketOps_OpHash",
                table: "TransferTicketOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTicketOps_SenderId",
                table: "TransferTicketOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTicketOps_TargetId",
                table: "TransferTicketOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferTicketOps_TicketerId",
                table: "TransferTicketOps",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupCommitOps_Level",
                table: "TxRollupCommitOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupCommitOps_OpHash",
                table: "TxRollupCommitOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupCommitOps_RollupId",
                table: "TxRollupCommitOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupCommitOps_SenderId",
                table: "TxRollupCommitOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupDispatchTicketsOps_Level",
                table: "TxRollupDispatchTicketsOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupDispatchTicketsOps_OpHash",
                table: "TxRollupDispatchTicketsOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupDispatchTicketsOps_RollupId",
                table: "TxRollupDispatchTicketsOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupDispatchTicketsOps_SenderId",
                table: "TxRollupDispatchTicketsOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupFinalizeCommitmentOps_Level",
                table: "TxRollupFinalizeCommitmentOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupFinalizeCommitmentOps_OpHash",
                table: "TxRollupFinalizeCommitmentOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupFinalizeCommitmentOps_RollupId",
                table: "TxRollupFinalizeCommitmentOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupFinalizeCommitmentOps_SenderId",
                table: "TxRollupFinalizeCommitmentOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupOriginationOps_Level",
                table: "TxRollupOriginationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupOriginationOps_OpHash",
                table: "TxRollupOriginationOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupOriginationOps_RollupId",
                table: "TxRollupOriginationOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupOriginationOps_SenderId",
                table: "TxRollupOriginationOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRejectionOps_CommitterId",
                table: "TxRollupRejectionOps",
                column: "CommitterId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRejectionOps_Level",
                table: "TxRollupRejectionOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRejectionOps_OpHash",
                table: "TxRollupRejectionOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRejectionOps_RollupId",
                table: "TxRollupRejectionOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRejectionOps_SenderId",
                table: "TxRollupRejectionOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRemoveCommitmentOps_Level",
                table: "TxRollupRemoveCommitmentOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRemoveCommitmentOps_OpHash",
                table: "TxRollupRemoveCommitmentOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRemoveCommitmentOps_RollupId",
                table: "TxRollupRemoveCommitmentOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupRemoveCommitmentOps_SenderId",
                table: "TxRollupRemoveCommitmentOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupReturnBondOps_Level",
                table: "TxRollupReturnBondOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupReturnBondOps_OpHash",
                table: "TxRollupReturnBondOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupReturnBondOps_RollupId",
                table: "TxRollupReturnBondOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupReturnBondOps_SenderId",
                table: "TxRollupReturnBondOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupSubmitBatchOps_Level",
                table: "TxRollupSubmitBatchOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupSubmitBatchOps_OpHash",
                table: "TxRollupSubmitBatchOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupSubmitBatchOps_RollupId",
                table: "TxRollupSubmitBatchOps",
                column: "RollupId");

            migrationBuilder.CreateIndex(
                name: "IX_TxRollupSubmitBatchOps_SenderId",
                table: "TxRollupSubmitBatchOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_BakerId",
                table: "VdfRevelationOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_Cycle",
                table: "VdfRevelationOps",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_Level",
                table: "VdfRevelationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_OpHash",
                table: "VdfRevelationOps",
                column: "OpHash");

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
                name: "FK_Blocks_Accounts_ProposerId",
                table: "Blocks",
                column: "ProposerId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_NonceRevelationOps_RevelationId",
                table: "Blocks",
                column: "RevelationId",
                principalTable: "NonceRevelationOps",
                principalColumn: "Id",
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
                name: "DoublePreendorsingOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "EndorsingRewardOps");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "FreezerUpdates");

            migrationBuilder.DropTable(
                name: "IncreasePaidStorageOps");

            migrationBuilder.DropTable(
                name: "MigrationOps");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "PreendorsementOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RegisterConstantOps");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "RevelationPenaltyOps");

            migrationBuilder.DropTable(
                name: "SetDepositsLimitOps");

            migrationBuilder.DropTable(
                name: "SnapshotBalances");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropTable(
                name: "TokenBalances");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "TokenTransfers");

            migrationBuilder.DropTable(
                name: "TransactionOps");

            migrationBuilder.DropTable(
                name: "TransferTicketOps");

            migrationBuilder.DropTable(
                name: "TxRollupCommitOps");

            migrationBuilder.DropTable(
                name: "TxRollupDispatchTicketsOps");

            migrationBuilder.DropTable(
                name: "TxRollupFinalizeCommitmentOps");

            migrationBuilder.DropTable(
                name: "TxRollupOriginationOps");

            migrationBuilder.DropTable(
                name: "TxRollupRejectionOps");

            migrationBuilder.DropTable(
                name: "TxRollupRemoveCommitmentOps");

            migrationBuilder.DropTable(
                name: "TxRollupReturnBondOps");

            migrationBuilder.DropTable(
                name: "TxRollupSubmitBatchOps");

            migrationBuilder.DropTable(
                name: "VdfRevelationOps");

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
