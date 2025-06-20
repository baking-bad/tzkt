﻿using System;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    SmartRollupBonds = table.Column<long>(type: "bigint", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    DelegationLevel = table.Column<int>(type: "integer", nullable: true),
                    Staked = table.Column<bool>(type: "boolean", nullable: false),
                    ContractsCount = table.Column<int>(type: "integer", nullable: false),
                    RollupsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupsCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveTokensCount = table.Column<int>(type: "integer", nullable: false),
                    TokenBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TokenTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveTicketsCount = table.Column<int>(type: "integer", nullable: false),
                    TicketBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TicketTransfersCount = table.Column<int>(type: "integer", nullable: false),
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
                    UpdateConsensusKeyCount = table.Column<int>(type: "integer", nullable: false),
                    DrainDelegateCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupAddMessagesCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupCementCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupExecuteCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupOriginateCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupPublishCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupRecoverBondCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupRefuteCount = table.Column<int>(type: "integer", nullable: false),
                    RefutationGamesCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveRefutationGamesCount = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Kind = table.Column<byte>(type: "smallint", nullable: true),
                    TypeHash = table.Column<int>(type: "integer", nullable: true),
                    CodeHash = table.Column<int>(type: "integer", nullable: true),
                    Tags = table.Column<int>(type: "integer", nullable: true),
                    TokensCount = table.Column<int>(type: "integer", nullable: true),
                    EventsCount = table.Column<int>(type: "integer", nullable: true),
                    TicketsCount = table.Column<int>(type: "integer", nullable: true),
                    CreatorId = table.Column<int>(type: "integer", nullable: true),
                    PvmKind = table.Column<int>(type: "integer", nullable: true),
                    ParameterSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    GenesisCommitment = table.Column<string>(type: "text", nullable: true),
                    LastCommitment = table.Column<string>(type: "text", nullable: true),
                    InboxLevel = table.Column<int>(type: "integer", nullable: true),
                    TotalStakers = table.Column<int>(type: "integer", nullable: true),
                    ActiveStakers = table.Column<int>(type: "integer", nullable: true),
                    ExecutedCommitments = table.Column<int>(type: "integer", nullable: true),
                    CementedCommitments = table.Column<int>(type: "integer", nullable: true),
                    PendingCommitments = table.Column<int>(type: "integer", nullable: true),
                    RefutedCommitments = table.Column<int>(type: "integer", nullable: true),
                    OrphanCommitments = table.Column<int>(type: "integer", nullable: true),
                    Revealed = table.Column<bool>(type: "boolean", nullable: true),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    StakedPseudotokens = table.Column<BigInteger>(type: "numeric", nullable: true),
                    UnstakedBalance = table.Column<long>(type: "bigint", nullable: true),
                    UnstakedBakerId = table.Column<int>(type: "integer", nullable: true),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
                    ActivationsCount = table.Column<int>(type: "integer", nullable: true),
                    RegisterConstantsCount = table.Column<int>(type: "integer", nullable: true),
                    SetDepositsLimitsCount = table.Column<int>(type: "integer", nullable: true),
                    StakingOpsCount = table.Column<int>(type: "integer", nullable: true),
                    SetDelegateParametersOpsCount = table.Column<int>(type: "integer", nullable: true),
                    DalPublishCommitmentOpsCount = table.Column<int>(type: "integer", nullable: true),
                    ActivationLevel = table.Column<int>(type: "integer", nullable: true),
                    DeactivationLevel = table.Column<int>(type: "integer", nullable: true),
                    StakingBalance = table.Column<long>(type: "bigint", nullable: true),
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: true),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: true),
                    OwnStakedBalance = table.Column<long>(type: "bigint", nullable: true),
                    ExternalStakedBalance = table.Column<long>(type: "bigint", nullable: true),
                    IssuedPseudotokens = table.Column<BigInteger>(type: "numeric", nullable: true),
                    StakersCount = table.Column<int>(type: "integer", nullable: true),
                    ExternalUnstakedBalance = table.Column<long>(type: "bigint", nullable: true),
                    RoundingError = table.Column<long>(type: "bigint", nullable: true),
                    FrozenDepositLimit = table.Column<long>(type: "bigint", nullable: true),
                    LimitOfStakingOverBaking = table.Column<long>(type: "bigint", nullable: true),
                    EdgeOfBakingOverStaking = table.Column<long>(type: "bigint", nullable: true),
                    BlocksCount = table.Column<int>(type: "integer", nullable: true),
                    EndorsementsCount = table.Column<int>(type: "integer", nullable: true),
                    PreendorsementsCount = table.Column<int>(type: "integer", nullable: true),
                    BallotsCount = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    DalEntrapmentEvidenceOpsCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleBakingCount = table.Column<int>(type: "integer", nullable: true),
                    DoubleEndorsingCount = table.Column<int>(type: "integer", nullable: true),
                    DoublePreendorsingCount = table.Column<int>(type: "integer", nullable: true),
                    NonceRevelationsCount = table.Column<int>(type: "integer", nullable: true),
                    VdfRevelationsCount = table.Column<int>(type: "integer", nullable: true),
                    RevelationPenaltiesCount = table.Column<int>(type: "integer", nullable: true),
                    EndorsingRewardsCount = table.Column<int>(type: "integer", nullable: true),
                    DalAttestationRewardsCount = table.Column<int>(type: "integer", nullable: true),
                    AutostakingOpsCount = table.Column<int>(type: "integer", nullable: true),
                    SoftwareId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
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
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Chain = table.Column<string>(type: "text", nullable: false),
                    ChainId = table.Column<string>(type: "text", nullable: false),
                    KnownHead = table.Column<int>(type: "integer", nullable: false),
                    LastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Protocol = table.Column<string>(type: "text", nullable: false),
                    NextProtocol = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    VotingEpoch = table.Column<int>(type: "integer", nullable: false),
                    VotingPeriod = table.Column<int>(type: "integer", nullable: false),
                    PendingDelegateParameters = table.Column<int>(type: "integer", nullable: false),
                    AccountCounter = table.Column<int>(type: "integer", nullable: false),
                    OperationCounter = table.Column<long>(type: "bigint", nullable: false),
                    ManagerCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapKeyCounter = table.Column<int>(type: "integer", nullable: false),
                    BigMapUpdateCounter = table.Column<int>(type: "integer", nullable: false),
                    StorageCounter = table.Column<int>(type: "integer", nullable: false),
                    ScriptCounter = table.Column<int>(type: "integer", nullable: false),
                    EventCounter = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupCommitmentCounter = table.Column<int>(type: "integer", nullable: false),
                    RefutationGameCounter = table.Column<int>(type: "integer", nullable: false),
                    InboxMessageCounter = table.Column<int>(type: "integer", nullable: false),
                    ProposalCounter = table.Column<int>(type: "integer", nullable: false),
                    SoftwareCounter = table.Column<int>(type: "integer", nullable: false),
                    CommitmentsCount = table.Column<int>(type: "integer", nullable: false),
                    BlocksCount = table.Column<int>(type: "integer", nullable: false),
                    ProtocolsCount = table.Column<int>(type: "integer", nullable: false),
                    ActivationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    BallotOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DelegationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DalEntrapmentEvidenceOpsCount = table.Column<int>(type: "integer", nullable: false),
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
                    StakingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SetDelegateParametersOpsCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RegisterConstantOpsCount = table.Column<int>(type: "integer", nullable: false),
                    EndorsingRewardOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DalAttestationRewardOpsCount = table.Column<int>(type: "integer", nullable: false),
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
                    UpdateConsensusKeyOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DrainDelegateOpsCount = table.Column<int>(type: "integer", nullable: false),
                    MigrationOpsCount = table.Column<int>(type: "integer", nullable: false),
                    RevelationPenaltyOpsCount = table.Column<int>(type: "integer", nullable: false),
                    AutostakingOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupAddMessagesOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupCementOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupExecuteOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupOriginateOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupPublishOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupRecoverBondOpsCount = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupRefuteOpsCount = table.Column<int>(type: "integer", nullable: false),
                    DalPublishCommitmentOpsCount = table.Column<int>(type: "integer", nullable: false),
                    CyclesCount = table.Column<int>(type: "integer", nullable: false),
                    ConstantsCount = table.Column<int>(type: "integer", nullable: false),
                    TokensCount = table.Column<int>(type: "integer", nullable: false),
                    TokenBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TokenTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    TicketsCount = table.Column<int>(type: "integer", nullable: false),
                    TicketBalancesCount = table.Column<int>(type: "integer", nullable: false),
                    TicketTransfersCount = table.Column<int>(type: "integer", nullable: false),
                    EventsCount = table.Column<int>(type: "integer", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: false),
                    UnstakeRequestsCount = table.Column<int>(type: "integer", nullable: false),
                    QuoteLevel = table.Column<int>(type: "integer", nullable: false),
                    QuoteBtc = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEur = table.Column<double>(type: "double precision", nullable: false),
                    QuoteUsd = table.Column<double>(type: "double precision", nullable: false),
                    QuoteCny = table.Column<double>(type: "double precision", nullable: false),
                    QuoteJpy = table.Column<double>(type: "double precision", nullable: false),
                    QuoteKrw = table.Column<double>(type: "double precision", nullable: false),
                    QuoteEth = table.Column<double>(type: "double precision", nullable: false),
                    QuoteGbp = table.Column<double>(type: "double precision", nullable: false),
                    DomainsNameRegistry = table.Column<string>(type: "text", nullable: false),
                    DomainsLevel = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutostakingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutostakingOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakerCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    OwnDelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    ExternalDelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: false),
                    OwnStakedBalance = table.Column<long>(type: "bigint", nullable: false),
                    ExternalStakedBalance = table.Column<long>(type: "bigint", nullable: false),
                    StakersCount = table.Column<int>(type: "integer", nullable: false),
                    BakingPower = table.Column<long>(type: "bigint", nullable: false),
                    TotalBakingPower = table.Column<long>(type: "bigint", nullable: false),
                    FutureBlocks = table.Column<int>(type: "integer", nullable: false),
                    Blocks = table.Column<int>(type: "integer", nullable: false),
                    MissedBlocks = table.Column<int>(type: "integer", nullable: false),
                    FutureEndorsements = table.Column<int>(type: "integer", nullable: false),
                    Endorsements = table.Column<int>(type: "integer", nullable: false),
                    MissedEndorsements = table.Column<int>(type: "integer", nullable: false),
                    FutureBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedBlockRewards = table.Column<long>(type: "bigint", nullable: false),
                    BlockRewardsDelegated = table.Column<long>(type: "bigint", nullable: false),
                    BlockRewardsStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    BlockRewardsStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    BlockRewardsStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    FutureEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedEndorsementRewards = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewardsDelegated = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewardsStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewardsStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewardsStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    FutureDalAttestationRewards = table.Column<long>(type: "bigint", nullable: false),
                    MissedDalAttestationRewards = table.Column<long>(type: "bigint", nullable: false),
                    DalAttestationRewardsDelegated = table.Column<long>(type: "bigint", nullable: false),
                    DalAttestationRewardsStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    DalAttestationRewardsStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    DalAttestationRewardsStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    BlockFees = table.Column<long>(type: "bigint", nullable: false),
                    MissedBlockFees = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleBakingLostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoubleEndorsingLostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingRewards = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingLostStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingLostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingLostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    DoublePreendorsingLostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    VdfRevelationRewardsDelegated = table.Column<long>(type: "bigint", nullable: false),
                    VdfRevelationRewardsStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    VdfRevelationRewardsStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    VdfRevelationRewardsStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationRewardsDelegated = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationRewardsStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationRewardsStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationRewardsStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationLosses = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedBlocks = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedEndorsements = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedDalShards = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakerCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BakingRights",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: true),
                    Slots = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BakingRights", x => x.Id);
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
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallotOps", x => x.Id);
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
                    KeyHash = table.Column<string>(type: "character(54)", fixedLength: true, maxLength: 54, nullable: false),
                    RawKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    JsonKey = table.Column<string>(type: "jsonb", nullable: false),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: false)
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
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    KeyType = table.Column<byte[]>(type: "bytea", nullable: false),
                    ValueType = table.Column<byte[]>(type: "bytea", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "BigMapUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigMapPtr = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
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
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProtoCode = table.Column<int>(type: "integer", nullable: false),
                    SoftwareId = table.Column<int>(type: "integer", nullable: true),
                    PayloadRound = table.Column<int>(type: "integer", nullable: false),
                    BlockRound = table.Column<int>(type: "integer", nullable: false),
                    Validations = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<int>(type: "integer", nullable: false),
                    Operations = table.Column<long>(type: "bigint", nullable: false),
                    Deposit = table.Column<long>(type: "bigint", nullable: false),
                    RewardDelegated = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    BonusDelegated = table.Column<long>(type: "bigint", nullable: false),
                    BonusStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    BonusStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    BonusStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    Fees = table.Column<long>(type: "bigint", nullable: false),
                    ProposerId = table.Column<int>(type: "integer", nullable: true),
                    ProducerId = table.Column<int>(type: "integer", nullable: true),
                    RevelationId = table.Column<long>(type: "bigint", nullable: true),
                    ResetBakerDeactivation = table.Column<int>(type: "integer", nullable: true),
                    ResetProposerDeactivation = table.Column<int>(type: "integer", nullable: true),
                    LBToggle = table.Column<bool>(type: "boolean", nullable: true),
                    LBToggleEma = table.Column<int>(type: "integer", nullable: false),
                    AIToggle = table.Column<bool>(type: "boolean", nullable: true),
                    AIToggleEma = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
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
                    Seed = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    SnapshotLevel = table.Column<int>(type: "integer", nullable: false),
                    TotalBakers = table.Column<int>(type: "integer", nullable: false),
                    TotalBakingPower = table.Column<long>(type: "bigint", nullable: false),
                    BlockReward = table.Column<long>(type: "bigint", nullable: false),
                    BlockBonusPerSlot = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementRewardPerSlot = table.Column<long>(type: "bigint", nullable: false),
                    NonceRevelationReward = table.Column<long>(type: "bigint", nullable: false),
                    VdfRevelationReward = table.Column<long>(type: "bigint", nullable: false),
                    DalAttestationRewardPerShard = table.Column<long>(type: "bigint", nullable: false),
                    MaxBlockReward = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "DalPublishCommitmentOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Commitment = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_DalPublishCommitmentOps", x => x.Id);
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
                    PrevDelegationLevel = table.Column<int>(type: "integer", nullable: true),
                    PrevDeactivationLevel = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
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
                    Errors = table.Column<string>(type: "text", nullable: true),
                    InitiatorId = table.Column<int>(type: "integer", nullable: true),
                    Nonce = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegationOps", x => x.Id);
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
                    DelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    StakedBalance = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatorCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Reverse = table.Column<bool>(type: "boolean", nullable: false),
                    Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoubleBakingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    SlashedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    LostStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleBakingOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoubleEndorsingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    SlashedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    LostStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoubleEndorsingOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoublePreendorsingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccusedLevel = table.Column<int>(type: "integer", nullable: false),
                    SlashedLevel = table.Column<int>(type: "integer", nullable: false),
                    AccuserId = table.Column<int>(type: "integer", nullable: false),
                    OffenderId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    LostStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalStaked = table.Column<long>(type: "bigint", nullable: false),
                    LostExternalUnstaked = table.Column<long>(type: "bigint", nullable: false),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoublePreendorsingOps", x => x.Id);
                });

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
                    AllocationFee = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrainDelegateOps", x => x.Id);
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
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndorsementOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EndorsingRewardOps",
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
                name: "InboxMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PredecessorLevel = table.Column<int>(type: "integer", nullable: true),
                    OperationId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    Protocol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncreasePaidStorageOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_IncreasePaidStorageOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MigrationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
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
                    RewardDelegated = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonceRevelationOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OriginationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderCodeHash = table.Column<int>(type: "integer", nullable: true),
                    DelegateId = table.Column<int>(type: "integer", nullable: true),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    ContractCodeHash = table.Column<int>(type: "integer", nullable: true),
                    ScriptId = table.Column<int>(type: "integer", nullable: true),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "PreendorsementOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    Slots = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreendorsementOps", x => x.Id);
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
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    InitiatorId = table.Column<int>(type: "integer", nullable: false),
                    FirstPeriod = table.Column<int>(type: "integer", nullable: false),
                    LastPeriod = table.Column<int>(type: "integer", nullable: false),
                    Epoch = table.Column<int>(type: "integer", nullable: false),
                    Upvotes = table.Column<int>(type: "integer", nullable: false),
                    VotingPower = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true)
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
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    FirstCycle = table.Column<int>(type: "integer", nullable: false),
                    FirstCycleLevel = table.Column<int>(type: "integer", nullable: false),
                    RampUpCycles = table.Column<int>(type: "integer", nullable: false),
                    NoRewardCycles = table.Column<int>(type: "integer", nullable: false),
                    ConsensusRightsDelay = table.Column<int>(type: "integer", nullable: false),
                    DelegateParametersActivationDelay = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerCycle = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerCommitment = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerSnapshot = table.Column<int>(type: "integer", nullable: false),
                    BlocksPerVoting = table.Column<int>(type: "integer", nullable: false),
                    TimeBetweenBlocks = table.Column<int>(type: "integer", nullable: false),
                    EndorsersPerBlock = table.Column<int>(type: "integer", nullable: false),
                    HardOperationGasLimit = table.Column<int>(type: "integer", nullable: false),
                    HardOperationStorageLimit = table.Column<int>(type: "integer", nullable: false),
                    HardBlockGasLimit = table.Column<int>(type: "integer", nullable: false),
                    MinimalStake = table.Column<long>(type: "bigint", nullable: false),
                    MinimalFrozenStake = table.Column<long>(type: "bigint", nullable: false),
                    BlockDeposit = table.Column<long>(type: "bigint", nullable: false),
                    BlockReward0 = table.Column<long>(type: "bigint", nullable: false),
                    BlockReward1 = table.Column<long>(type: "bigint", nullable: false),
                    MaxBakingReward = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementDeposit = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementReward0 = table.Column<long>(type: "bigint", nullable: false),
                    EndorsementReward1 = table.Column<long>(type: "bigint", nullable: false),
                    MaxEndorsingReward = table.Column<long>(type: "bigint", nullable: false),
                    OriginationSize = table.Column<int>(type: "integer", nullable: false),
                    ByteCost = table.Column<int>(type: "integer", nullable: false),
                    ProposalQuorum = table.Column<int>(type: "integer", nullable: false),
                    BallotQuorumMin = table.Column<int>(type: "integer", nullable: false),
                    BallotQuorumMax = table.Column<int>(type: "integer", nullable: false),
                    LBToggleThreshold = table.Column<int>(type: "integer", nullable: false),
                    ConsensusThreshold = table.Column<int>(type: "integer", nullable: false),
                    MinParticipationNumerator = table.Column<int>(type: "integer", nullable: false),
                    MinParticipationDenominator = table.Column<int>(type: "integer", nullable: false),
                    DenunciationPeriod = table.Column<int>(type: "integer", nullable: false),
                    SlashingDelay = table.Column<int>(type: "integer", nullable: false),
                    MaxDelegatedOverFrozenRatio = table.Column<int>(type: "integer", nullable: false),
                    MaxExternalOverOwnStakeRatio = table.Column<int>(type: "integer", nullable: false),
                    StakePowerMultiplier = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupOriginationSize = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupStakeAmount = table.Column<long>(type: "bigint", nullable: false),
                    SmartRollupChallengeWindow = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupCommitmentPeriod = table.Column<int>(type: "integer", nullable: false),
                    SmartRollupTimeoutPeriod = table.Column<int>(type: "integer", nullable: false),
                    Dictator = table.Column<string>(type: "text", nullable: true),
                    DoubleBakingSlashedPercentage = table.Column<int>(type: "integer", nullable: false),
                    DoubleEndorsingSlashedPercentage = table.Column<int>(type: "integer", nullable: false),
                    NumberOfShards = table.Column<int>(type: "integer", nullable: false),
                    ToleratedInactivityPeriod = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "RegisterConstantOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                    Value = table.Column<byte[]>(type: "bytea", nullable: true),
                    Refs = table.Column<int>(type: "integer", nullable: true),
                    Extras = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_RegisterConstantOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevealOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("PK_RevealOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevelationPenaltyOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    MissedLevel = table.Column<int>(type: "integer", nullable: false),
                    Loss = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevelationPenaltyOps", x => x.Id);
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
                    ParameterSchema = table.Column<byte[]>(type: "bytea", nullable: false),
                    StorageSchema = table.Column<byte[]>(type: "bytea", nullable: false),
                    CodeSchema = table.Column<byte[]>(type: "bytea", nullable: false),
                    Views = table.Column<byte[][]>(type: "bytea[]", nullable: true),
                    TypeHash = table.Column<int>(type: "integer", nullable: false),
                    CodeHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetDelegateParametersOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LimitOfStakingOverBaking = table.Column<long>(type: "bigint", nullable: true),
                    EdgeOfBakingOverStaking = table.Column<long>(type: "bigint", nullable: true),
                    ActivationCycle = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_SetDelegateParametersOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetDepositsLimitOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Limit = table.Column<BigInteger>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_SetDepositsLimitOps", x => x.Id);
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
                    State = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
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
                    TicketTransfers = table.Column<int>(type: "integer", nullable: true),
                    SubIds = table.Column<int>(type: "integer", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "SmartRollupOriginateOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PvmKind = table.Column<int>(type: "integer", nullable: false),
                    Kernel = table.Column<byte[]>(type: "bytea", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "SnapshotBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    DelegatorsCount = table.Column<int>(type: "integer", nullable: false),
                    OwnDelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    ExternalDelegatedBalance = table.Column<long>(type: "bigint", nullable: false),
                    OwnStakedBalance = table.Column<long>(type: "bigint", nullable: false),
                    ExternalStakedBalance = table.Column<long>(type: "bigint", nullable: false),
                    StakersCount = table.Column<int>(type: "integer", nullable: false)
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
                    ShortHash = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    BlocksCount = table.Column<int>(type: "integer", nullable: false),
                    Extras = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StakingOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    RequestedAmount = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: true),
                    BakerId = table.Column<int>(type: "integer", nullable: true),
                    StakingUpdatesCount = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_StakingOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StakingUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    StakerId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Pseudotokens = table.Column<BigInteger>(type: "numeric", nullable: true),
                    RoundingError = table.Column<long>(type: "bigint", nullable: true),
                    AutostakingOpId = table.Column<long>(type: "bigint", nullable: true),
                    StakingOpId = table.Column<long>(type: "bigint", nullable: true),
                    DelegationOpId = table.Column<long>(type: "bigint", nullable: true),
                    DoubleBakingOpId = table.Column<long>(type: "bigint", nullable: true),
                    DoubleEndorsingOpId = table.Column<long>(type: "bigint", nullable: true),
                    DoublePreendorsingOpId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakingUpdates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalBootstrapped = table.Column<long>(type: "bigint", nullable: false),
                    TotalCommitments = table.Column<long>(type: "bigint", nullable: false),
                    TotalActivated = table.Column<long>(type: "bigint", nullable: false),
                    TotalCreated = table.Column<long>(type: "bigint", nullable: false),
                    TotalBurned = table.Column<long>(type: "bigint", nullable: false),
                    TotalBanished = table.Column<long>(type: "bigint", nullable: false),
                    TotalLost = table.Column<long>(type: "bigint", nullable: false),
                    TotalFrozen = table.Column<long>(type: "bigint", nullable: false),
                    TotalRollupBonds = table.Column<long>(type: "bigint", nullable: false),
                    TotalSmartRollupBonds = table.Column<long>(type: "bigint", nullable: false)
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
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: false),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<BigInteger>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    FirstMinterId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    BalancesCount = table.Column<int>(type: "integer", nullable: false),
                    HoldersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMinted = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalBurned = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalSupply = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TypeHash = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<int>(type: "integer", nullable: false),
                    RawType = table.Column<byte[]>(type: "bytea", nullable: false),
                    RawContent = table.Column<byte[]>(type: "bytea", nullable: false),
                    JsonContent = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketTransfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    TicketerId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
                    FromId = table.Column<int>(type: "integer", nullable: true),
                    ToId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<long>(type: "bigint", nullable: true),
                    TransferTicketId = table.Column<long>(type: "bigint", nullable: true),
                    SmartRollupExecuteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TokenId = table.Column<long>(type: "bigint", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<BigInteger>(type: "numeric", nullable: false),
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
                    TokenId = table.Column<BigInteger>(type: "numeric", nullable: false),
                    Tags = table.Column<int>(type: "integer", nullable: false),
                    FirstMinterId = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TransfersCount = table.Column<int>(type: "integer", nullable: false),
                    BalancesCount = table.Column<int>(type: "integer", nullable: false),
                    HoldersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMinted = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalBurned = table.Column<BigInteger>(type: "numeric", nullable: false),
                    TotalSupply = table.Column<BigInteger>(type: "numeric", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    IndexedAt = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: false)
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
                    TokenId = table.Column<long>(type: "bigint", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
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
                    TicketTransfers = table.Column<int>(type: "integer", nullable: true),
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
                });

            migrationBuilder.CreateTable(
                name: "TransferTicketOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: true),
                    TicketerId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<BigInteger>(type: "numeric", nullable: false),
                    RawType = table.Column<byte[]>(type: "bytea", nullable: true),
                    RawContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonContent = table.Column<string>(type: "text", nullable: true),
                    Entrypoint = table.Column<string>(type: "text", nullable: false),
                    TicketTransfers = table.Column<int>(type: "integer", nullable: true),
                    SubIds = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TransferTicketOps", x => x.Id);
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
                    table.PrimaryKey("PK_TxRollupCommitOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupDispatchTicketsOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupDispatchTicketsOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupFinalizeCommitmentOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupFinalizeCommitmentOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupOriginationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupOriginationOps", x => x.Id);
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
                    table.PrimaryKey("PK_TxRollupRejectionOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupRemoveCommitmentOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupRemoveCommitmentOps", x => x.Id);
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
                    table.PrimaryKey("PK_TxRollupReturnBondOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TxRollupSubmitBatchOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RollupId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_TxRollupSubmitBatchOps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnstakeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    StakerId = table.Column<int>(type: "integer", nullable: true),
                    RequestedAmount = table.Column<long>(type: "bigint", nullable: false),
                    RestakedAmount = table.Column<long>(type: "bigint", nullable: false),
                    FinalizedAmount = table.Column<long>(type: "bigint", nullable: false),
                    SlashedAmount = table.Column<long>(type: "bigint", nullable: false),
                    RoundingError = table.Column<long>(type: "bigint", nullable: true),
                    UpdatesCount = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnstakeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateConsensusKeyOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivationCycle = table.Column<int>(type: "integer", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    PublicKeyHash = table.Column<string>(type: "text", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "VdfRevelationOps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    RewardDelegated = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedOwn = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedEdge = table.Column<long>(type: "bigint", nullable: false),
                    RewardStakedShared = table.Column<long>(type: "bigint", nullable: false),
                    Solution = table.Column<byte[]>(type: "bytea", nullable: false),
                    Proof = table.Column<byte[]>(type: "bytea", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VdfRevelationOps", x => x.Id);
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
                    TotalBakers = table.Column<int>(type: "integer", nullable: false),
                    TotalVotingPower = table.Column<long>(type: "bigint", nullable: false),
                    UpvotesQuorum = table.Column<int>(type: "integer", nullable: true),
                    ProposalsCount = table.Column<int>(type: "integer", nullable: true),
                    TopUpvotes = table.Column<int>(type: "integer", nullable: true),
                    TopVotingPower = table.Column<long>(type: "bigint", nullable: true),
                    SingleWinner = table.Column<bool>(type: "boolean", nullable: true),
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

            migrationBuilder.InsertData(
                table: "AppState",
                columns: new[] { "Id", "AccountCounter", "ActivationOpsCount", "AutostakingOpsCount", "BallotOpsCount", "BigMapCounter", "BigMapKeyCounter", "BigMapUpdateCounter", "BlocksCount", "Chain", "ChainId", "CommitmentsCount", "ConstantsCount", "Cycle", "CyclesCount", "DalAttestationRewardOpsCount", "DalEntrapmentEvidenceOpsCount", "DalPublishCommitmentOpsCount", "DelegationOpsCount", "DomainsLevel", "DomainsNameRegistry", "DoubleBakingOpsCount", "DoubleEndorsingOpsCount", "DoublePreendorsingOpsCount", "DrainDelegateOpsCount", "EndorsementOpsCount", "EndorsingRewardOpsCount", "EventCounter", "EventsCount", "Extras", "Hash", "InboxMessageCounter", "IncreasePaidStorageOpsCount", "KnownHead", "LastSync", "Level", "ManagerCounter", "MigrationOpsCount", "NextProtocol", "NonceRevelationOpsCount", "OperationCounter", "OriginationOpsCount", "PendingDelegateParameters", "PreendorsementOpsCount", "ProposalCounter", "ProposalOpsCount", "Protocol", "ProtocolsCount", "QuoteBtc", "QuoteCny", "QuoteEth", "QuoteEur", "QuoteGbp", "QuoteJpy", "QuoteKrw", "QuoteLevel", "QuoteUsd", "RefutationGameCounter", "RegisterConstantOpsCount", "RevealOpsCount", "RevelationPenaltyOpsCount", "ScriptCounter", "SetDelegateParametersOpsCount", "SetDepositsLimitOpsCount", "SmartRollupAddMessagesOpsCount", "SmartRollupCementOpsCount", "SmartRollupCommitmentCounter", "SmartRollupExecuteOpsCount", "SmartRollupOriginateOpsCount", "SmartRollupPublishOpsCount", "SmartRollupRecoverBondOpsCount", "SmartRollupRefuteOpsCount", "SoftwareCounter", "StakingOpsCount", "StakingUpdatesCount", "StorageCounter", "TicketBalancesCount", "TicketTransfersCount", "TicketsCount", "Timestamp", "TokenBalancesCount", "TokenTransfersCount", "TokensCount", "TransactionOpsCount", "TransferTicketOpsCount", "TxRollupCommitOpsCount", "TxRollupDispatchTicketsOpsCount", "TxRollupFinalizeCommitmentOpsCount", "TxRollupOriginationOpsCount", "TxRollupRejectionOpsCount", "TxRollupRemoveCommitmentOpsCount", "TxRollupReturnBondOpsCount", "TxRollupSubmitBatchOpsCount", "UnstakeRequestsCount", "UpdateConsensusKeyOpsCount", "VdfRevelationOpsCount", "VotingEpoch", "VotingPeriod" },
                values: new object[] { -1, 0, 0, 0, 0, 0, 0, 0, 0, "", "", 0, 0, -1, 0, 0, 0, 0, 0, 0, "", 0, 0, 0, 0, 0, 0, 0, 0, null, "", 0, 0, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), -1, 0, 0, "", 0, 0L, 0, 0, 0, 0, 0, "", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, -1, 0.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1 });

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
                name: "IX_Accounts_DeactivationLevel_Partial",
                table: "Accounts",
                column: "DeactivationLevel",
                filter: "\"Type\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DelegateId",
                table: "Accounts",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Extras",
                table: "Accounts",
                column: "Extras")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FirstLevel",
                table: "Accounts",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Kind_Partial",
                table: "Accounts",
                column: "Kind",
                filter: "\"Type\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Staked_Partial",
                table: "Accounts",
                column: "Staked",
                filter: "\"Type\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Staked_Type",
                table: "Accounts",
                columns: new[] { "Staked", "Type" },
                filter: "\"Staked\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Type",
                table: "Accounts",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TypeHash",
                table: "Accounts",
                column: "TypeHash");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UnstakedBakerId",
                table: "Accounts",
                column: "UnstakedBakerId",
                filter: "\"UnstakedBakerId\" IS NOT NULL");

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
                name: "IX_AutostakingOps_BakerId",
                table: "AutostakingOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_AutostakingOps_Level",
                table: "AutostakingOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_BakerId",
                table: "BakerCycles",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_BakerCycles_Cycle_BakerId",
                table: "BakerCycles",
                columns: new[] { "Cycle", "BakerId" });

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
                name: "IX_BigMapKeys_BigMapPtr_KeyHash",
                table: "BigMapKeys",
                columns: new[] { "BigMapPtr", "KeyHash" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr_Partial",
                table: "BigMapKeys",
                column: "BigMapPtr",
                filter: "\"Active\" = true");

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
                name: "IX_BigMaps_Ptr",
                table: "BigMaps",
                column: "Ptr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapKeyId_Id",
                table: "BigMapUpdates",
                columns: new[] { "BigMapKeyId", "Id" },
                filter: "\"BigMapKeyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapPtr_Id",
                table: "BigMapUpdates",
                columns: new[] { "BigMapPtr", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_Level",
                table: "BigMapUpdates",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_MigrationId",
                table: "BigMapUpdates",
                column: "MigrationId",
                filter: "\"MigrationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_OriginationId",
                table: "BigMapUpdates",
                column: "OriginationId",
                filter: "\"OriginationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_TransactionId",
                table: "BigMapUpdates",
                column: "TransactionId",
                filter: "\"TransactionId\" IS NOT NULL");

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
                name: "IX_Commitments_AccountId",
                table: "Commitments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_Address",
                table: "Commitments",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_Index",
                table: "Cycles",
                column: "Index",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_DalPublishCommitmentOps_Level",
                table: "DalPublishCommitmentOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_DalPublishCommitmentOps_OpHash",
                table: "DalPublishCommitmentOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_DalPublishCommitmentOps_SenderId",
                table: "DalPublishCommitmentOps",
                column: "SenderId");

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
                name: "IX_DelegationOps_SenderId_Id",
                table: "DelegationOps",
                columns: new[] { "SenderId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_BakerId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "BakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_Cycle_DelegatorId",
                table: "DelegatorCycles",
                columns: new[] { "Cycle", "DelegatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatorCycles_DelegatorId",
                table: "DelegatorCycles",
                column: "DelegatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Address",
                table: "Domains",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_FirstLevel",
                table: "Domains",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_LastLevel",
                table: "Domains",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Level",
                table: "Domains",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Name",
                table: "Domains",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Owner",
                table: "Domains",
                column: "Owner");

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
                name: "IX_Events_ContractCodeHash_Tag",
                table: "Events",
                columns: new[] { "ContractCodeHash", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractId_Tag",
                table: "Events",
                columns: new[] { "ContractId", "Tag" });

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
                name: "IX_InboxMessages_Level",
                table: "InboxMessages",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_OperationId",
                table: "InboxMessages",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_Type_Id",
                table: "InboxMessages",
                columns: new[] { "Type", "Id" });

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
                name: "IX_OriginationOps_OpHash",
                table: "OriginationOps",
                column: "OpHash");

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
                name: "IX_Proposals_FirstPeriod",
                table: "Proposals",
                column: "FirstPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_Hash",
                table: "Proposals",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_LastPeriod",
                table: "Proposals",
                column: "LastPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_Status_Partial",
                table: "Proposals",
                column: "Status",
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_Code",
                table: "Protocols",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_Hash",
                table: "Protocols",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Level",
                table: "Quotes",
                column: "Level",
                unique: true);

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
                name: "IX_RefutationGames_SmartRollupId_Id",
                table: "RefutationGames",
                columns: new[] { "SmartRollupId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_Address",
                table: "RegisterConstantOps",
                column: "Address",
                filter: "\"Address\" IS NOT NULL");

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
                name: "IX_Scripts_ContractId_Partial",
                table: "Scripts",
                column: "ContractId",
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_SetDelegateParametersOps_ActivationCycle",
                table: "SetDelegateParametersOps",
                column: "ActivationCycle");

            migrationBuilder.CreateIndex(
                name: "IX_SetDelegateParametersOps_Level",
                table: "SetDelegateParametersOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SetDelegateParametersOps_OpHash",
                table: "SetDelegateParametersOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SetDelegateParametersOps_SenderId_Id",
                table: "SetDelegateParametersOps",
                columns: new[] { "SenderId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SetDepositsLimitOps_Level",
                table: "SetDepositsLimitOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_SetDepositsLimitOps_OpHash",
                table: "SetDepositsLimitOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_SetDepositsLimitOps_SenderId_Id",
                table: "SetDepositsLimitOps",
                columns: new[] { "SenderId", "Id" });

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
                name: "IX_SmartRollupCementOps_CommitmentId",
                table: "SmartRollupCementOps",
                column: "CommitmentId");

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
                name: "IX_SmartRollupCementOps_SmartRollupId_Id",
                table: "SmartRollupCementOps",
                columns: new[] { "SmartRollupId", "Id" });

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
                name: "IX_SmartRollupExecuteOps_CommitmentId_Id",
                table: "SmartRollupExecuteOps",
                columns: new[] { "CommitmentId", "Id" });

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
                name: "IX_SmartRollupPublishOps_SmartRollupId_BondStatus_SenderId",
                table: "SmartRollupPublishOps",
                columns: new[] { "SmartRollupId", "BondStatus", "SenderId" },
                filter: "\"BondStatus\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SmartRollupPublishOps_SmartRollupId_SenderId_Id",
                table: "SmartRollupPublishOps",
                columns: new[] { "SmartRollupId", "SenderId", "Id" });

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
                name: "IX_SmartRollupRefuteOps_GameId_Id",
                table: "SmartRollupRefuteOps",
                columns: new[] { "GameId", "Id" });

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

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotBalances_Level_AccountId_BakerId",
                table: "SnapshotBalances",
                columns: new[] { "Level", "AccountId", "BakerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotBalances_Level_Partial",
                table: "SnapshotBalances",
                column: "Level",
                filter: "\"AccountId\" = \"BakerId\"");

            migrationBuilder.CreateIndex(
                name: "IX_StakingOps_BakerId",
                table: "StakingOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingOps_Level",
                table: "StakingOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_StakingOps_OpHash",
                table: "StakingOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_StakingOps_SenderId",
                table: "StakingOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_AutostakingOpId",
                table: "StakingUpdates",
                column: "AutostakingOpId",
                filter: "\"AutostakingOpId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_BakerId_Cycle_Id",
                table: "StakingUpdates",
                columns: new[] { "BakerId", "Cycle", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DelegationOpId",
                table: "StakingUpdates",
                column: "DelegationOpId",
                filter: "\"DelegationOpId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoubleBakingOpId",
                table: "StakingUpdates",
                column: "DoubleBakingOpId",
                filter: "\"DoubleBakingOpId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoubleEndorsingOpId",
                table: "StakingUpdates",
                column: "DoubleEndorsingOpId",
                filter: "\"DoubleEndorsingOpId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_DoublePreendorsingOpId",
                table: "StakingUpdates",
                column: "DoublePreendorsingOpId",
                filter: "\"DoublePreendorsingOpId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_Level_Id",
                table: "StakingUpdates",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_StakerId_Cycle_Id",
                table: "StakingUpdates",
                columns: new[] { "StakerId", "Cycle", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StakingUpdates_StakingOpId",
                table: "StakingUpdates",
                column: "StakingOpId",
                filter: "\"StakingOpId\" IS NOT NULL");

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
                name: "IX_Storages_ContractId_Id",
                table: "Storages",
                columns: new[] { "ContractId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Storages_ContractId_Partial",
                table: "Storages",
                column: "ContractId",
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Level",
                table: "Storages",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId_TicketerId",
                table: "TicketBalances",
                columns: new[] { "AccountId", "TicketerId" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_AccountId_TicketId",
                table: "TicketBalances",
                columns: new[] { "AccountId", "TicketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_FirstLevel",
                table: "TicketBalances",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_LastLevel",
                table: "TicketBalances",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketerId",
                table: "TicketBalances",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketBalances_TicketId",
                table: "TicketBalances",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ContentHash",
                table: "Tickets",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_FirstLevel",
                table: "Tickets",
                column: "FirstLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_FirstMinterId",
                table: "Tickets",
                column: "FirstMinterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_JsonContent",
                table: "Tickets",
                column: "JsonContent")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LastLevel",
                table: "Tickets",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketerId_TypeHash_ContentHash",
                table: "Tickets",
                columns: new[] { "TicketerId", "TypeHash", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TypeHash",
                table: "Tickets",
                column: "TypeHash");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_FromId",
                table: "TicketTransfers",
                column: "FromId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_Level_Id",
                table: "TicketTransfers",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_SmartRollupExecuteId",
                table: "TicketTransfers",
                column: "SmartRollupExecuteId",
                filter: "\"SmartRollupExecuteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TicketerId",
                table: "TicketTransfers",
                column: "TicketerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TicketId",
                table: "TicketTransfers",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_ToId",
                table: "TicketTransfers",
                column: "ToId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TransactionId",
                table: "TicketTransfers",
                column: "TransactionId",
                filter: "\"TransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTransfers_TransferTicketId",
                table: "TicketTransfers",
                column: "TransferTicketId",
                filter: "\"TransferTicketId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_ContractId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "ContractId" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_Partial",
                table: "TokenBalances",
                column: "AccountId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_AccountId_TokenId",
                table: "TokenBalances",
                columns: new[] { "AccountId", "TokenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_ContractId",
                table: "TokenBalances",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_ContractId_Partial",
                table: "TokenBalances",
                column: "ContractId",
                filter: "\"Balance\" != '0'");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_IndexedAt",
                table: "TokenBalances",
                column: "IndexedAt",
                filter: "\"IndexedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_LastLevel",
                table: "TokenBalances",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_TokenId",
                table: "TokenBalances",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalances_TokenId_Partial",
                table: "TokenBalances",
                column: "TokenId",
                filter: "\"Balance\" != '0'");

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
                name: "IX_Tokens_IndexedAt",
                table: "Tokens",
                column: "IndexedAt",
                filter: "\"IndexedAt\" IS NOT NULL");

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
                filter: "\"FromId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_IndexedAt",
                table: "TokenTransfers",
                column: "IndexedAt",
                filter: "\"IndexedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_Level_Id",
                table: "TokenTransfers",
                columns: new[] { "Level", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_MigrationId",
                table: "TokenTransfers",
                column: "MigrationId",
                filter: "\"MigrationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_OriginationId",
                table: "TokenTransfers",
                column: "OriginationId",
                filter: "\"OriginationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ToId",
                table: "TokenTransfers",
                column: "ToId",
                filter: "\"ToId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TokenId",
                table: "TokenTransfers",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TransactionId",
                table: "TokenTransfers",
                column: "TransactionId",
                filter: "\"TransactionId\" IS NOT NULL");

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
                name: "IX_TransactionOps_TargetCodeHash",
                table: "TransactionOps",
                column: "TargetCodeHash",
                filter: "\"TargetCodeHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId",
                table: "TransactionOps",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_TargetId_Partial",
                table: "TransactionOps",
                column: "TargetId",
                filter: "\"Entrypoint\" = 'transfer'\nAND \"TokenTransfers\" IS NULL\nAND \"Status\" = 1");

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
                name: "IX_UnstakeRequests_BakerId_Cycle",
                table: "UnstakeRequests",
                columns: new[] { "BakerId", "Cycle" });

            migrationBuilder.CreateIndex(
                name: "IX_UnstakeRequests_StakerId_Cycle",
                table: "UnstakeRequests",
                columns: new[] { "StakerId", "Cycle" });

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

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_BakerId",
                table: "VdfRevelationOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_Cycle_Id",
                table: "VdfRevelationOps",
                columns: new[] { "Cycle", "Id" });

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
                name: "IX_VotingPeriods_Index",
                table: "VotingPeriods",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingSnapshots_Period_BakerId",
                table: "VotingSnapshots",
                columns: new[] { "Period", "BakerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ActivationOps");

            migrationBuilder.DropTable(
                name: "AppState");

            migrationBuilder.DropTable(
                name: "AutostakingOps");

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
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Commitments");

            migrationBuilder.DropTable(
                name: "Cycles");

            migrationBuilder.DropTable(
                name: "DalAttestationRewardOps");

            migrationBuilder.DropTable(
                name: "DalEntrapmentEvidenceOps");

            migrationBuilder.DropTable(
                name: "DalPublishCommitmentOps");

            migrationBuilder.DropTable(
                name: "DelegationOps");

            migrationBuilder.DropTable(
                name: "DelegatorCycles");

            migrationBuilder.DropTable(
                name: "Domains");

            migrationBuilder.DropTable(
                name: "DoubleBakingOps");

            migrationBuilder.DropTable(
                name: "DoubleEndorsingOps");

            migrationBuilder.DropTable(
                name: "DoublePreendorsingOps");

            migrationBuilder.DropTable(
                name: "DrainDelegateOps");

            migrationBuilder.DropTable(
                name: "EndorsementOps");

            migrationBuilder.DropTable(
                name: "EndorsingRewardOps");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "IncreasePaidStorageOps");

            migrationBuilder.DropTable(
                name: "MigrationOps");

            migrationBuilder.DropTable(
                name: "NonceRevelationOps");

            migrationBuilder.DropTable(
                name: "OriginationOps");

            migrationBuilder.DropTable(
                name: "PreendorsementOps");

            migrationBuilder.DropTable(
                name: "ProposalOps");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RefutationGames");

            migrationBuilder.DropTable(
                name: "RegisterConstantOps");

            migrationBuilder.DropTable(
                name: "RevealOps");

            migrationBuilder.DropTable(
                name: "RevelationPenaltyOps");

            migrationBuilder.DropTable(
                name: "Scripts");

            migrationBuilder.DropTable(
                name: "SetDelegateParametersOps");

            migrationBuilder.DropTable(
                name: "SetDepositsLimitOps");

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

            migrationBuilder.DropTable(
                name: "SnapshotBalances");

            migrationBuilder.DropTable(
                name: "Software");

            migrationBuilder.DropTable(
                name: "StakingOps");

            migrationBuilder.DropTable(
                name: "StakingUpdates");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropTable(
                name: "TicketBalances");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketTransfers");

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
                name: "UnstakeRequests");

            migrationBuilder.DropTable(
                name: "UpdateConsensusKeyOps");

            migrationBuilder.DropTable(
                name: "VdfRevelationOps");

            migrationBuilder.DropTable(
                name: "VotingPeriods");

            migrationBuilder.DropTable(
                name: "VotingSnapshots");
        }
    }
}
