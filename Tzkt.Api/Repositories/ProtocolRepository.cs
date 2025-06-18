using Dapper;
using Npgsql;
using Tzkt.Api.Models;

namespace Tzkt.Api.Repositories
{
    public class ProtocolRepository
    {
        readonly NpgsqlDataSource DataSource;

        public ProtocolRepository(NpgsqlDataSource dataSource) => DataSource = dataSource;

        public async Task<int> GetCount()
        {
            var sql = @"
                SELECT   COUNT(*)
                FROM     ""Protocols""";

            await using var db = await DataSource.OpenConnectionAsync();
            return await db.QueryFirstAsync<int>(sql);
        }

        public async Task<Protocol?> GetByCycle(int cycle)
        {
            var sql = $@"
                SELECT      *
                FROM        ""Protocols""
                WHERE       ""FirstCycle"" <= {cycle}
                ORDER BY    ""FirstLevel"" DESC
                LIMIT       1";

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;

            return new Protocol
            {
                Code = row.Code,
                Hash = row.Hash,
                Version = row.Version,
                FirstLevel = row.FirstLevel,
                FirstCycle = row.FirstCycle,
                FirstCycleLevel = row.FirstCycleLevel,
                LastLevel = row.LastLevel == -1 ? null : row.LastLevel,
                Constants = new ProtocolConstants
                {
                    RampUpCycles = row.RampUpCycles,
                    NoRewardCycles = row.NoRewardCycles,
                    BlockDeposit = row.BlockDeposit,
                    BlockReward = row.BlockReward1 == 0
                        ? [row.BlockReward0]
                        : [row.BlockReward0, row.BlockReward1],
                    BlocksPerCommitment = row.BlocksPerCommitment,
                    BlocksPerCycle = row.BlocksPerCycle,
                    BlocksPerSnapshot = row.BlocksPerSnapshot,
                    BlocksPerVoting = row.BlocksPerVoting,
                    ByteCost = row.ByteCost,
                    AttestationDeposit = row.AttestationDeposit,
                    AttestationReward = row.AttestationReward1 == 0
                        ? [row.AttestationReward0]
                        : [row.AttestationReward0, row.AttestationReward1],
                    AttestersPerBlock = row.AttestersPerBlock,
                    HardBlockGasLimit = row.HardBlockGasLimit,
                    HardOperationGasLimit = row.HardOperationGasLimit,
                    HardOperationStorageLimit = row.HardOperationStorageLimit,
                    OriginationSize = row.OriginationSize,
                    ConsensusRightsDelay = row.ConsensusRightsDelay,
                    DelegateParametersActivationDelay = row.DelegateParametersActivationDelay,
                    TimeBetweenBlocks = row.TimeBetweenBlocks,
                    MinimalStake = row.MinimalStake,
                    MinimalFrozenStake = row.MinimalFrozenStake,
                    ProposalQuorum = row.ProposalQuorum / 100.0,
                    BallotQuorumMin = row.BallotQuorumMin / 100.0,
                    BallotQuorumMax = row.BallotQuorumMax / 100.0,
                    LBToggleThreshold = row.LBToggleThreshold,
                    ConsensusThreshold = row.ConsensusThreshold,
                    MaxDelegatedOverFrozenRatio = row.MaxDelegatedOverFrozenRatio,
                    MaxExternalOverOwnStakeRatio = row.MaxExternalOverOwnStakeRatio,
                    DenunciationPeriod = row.DenunciationPeriod,
                    SlashingDelay = row.SlashingDelay,
                    MinParticipationDenominator = row.MinParticipationDenominator,
                    MinParticipationNumerator = row.MinParticipationNumerator,
                    SmartRollupChallengeWindow= row.SmartRollupChallengeWindow,
                    SmartRollupCommitmentPeriod= row.SmartRollupCommitmentPeriod,
                    SmartRollupOriginationSize= row.SmartRollupOriginationSize,
                    SmartRollupStakeAmount= row.SmartRollupStakeAmount,
                    SmartRollupTimeoutPeriod= row.SmartRollupTimeoutPeriod,
                    DalNumberOfShards = row.NumberOfShards,
                    Dictator = row.Dictator
                },
                Extras = row.Extras
            };
        }

        public async Task<Protocol?> Get(int code)
        {
            var sql = @"
                SELECT  *
                FROM    ""Protocols""
                WHERE   ""Code"" = @code
                LIMIT   1";

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { code });
            if (row == null) return null;

            return new Protocol
            {
                Code = code,
                Hash = row.Hash,
                Version = row.Version,
                FirstLevel = row.FirstLevel,
                FirstCycle = row.FirstCycle,
                FirstCycleLevel = row.FirstCycleLevel,
                LastLevel = row.LastLevel == -1 ? null : row.LastLevel,
                Constants = new ProtocolConstants
                {
                    RampUpCycles = row.RampUpCycles,
                    NoRewardCycles = row.NoRewardCycles,
                    BlockDeposit = row.BlockDeposit,
                    BlockReward = row.BlockReward1 == 0
                        ? [row.BlockReward0]
                        : [row.BlockReward0, row.BlockReward1],
                    BlocksPerCommitment = row.BlocksPerCommitment,
                    BlocksPerCycle = row.BlocksPerCycle,
                    BlocksPerSnapshot = row.BlocksPerSnapshot,
                    BlocksPerVoting = row.BlocksPerVoting,
                    ByteCost = row.ByteCost,
                    AttestationDeposit = row.AttestationDeposit,
                    AttestationReward = row.AttestationReward1 == 0
                        ? [row.AttestationReward0]
                        : [row.AttestationReward0, row.AttestationReward1],
                    AttestersPerBlock = row.AttestersPerBlock,
                    HardBlockGasLimit = row.HardBlockGasLimit,
                    HardOperationGasLimit = row.HardOperationGasLimit,
                    HardOperationStorageLimit = row.HardOperationStorageLimit,
                    OriginationSize = row.OriginationSize,
                    ConsensusRightsDelay = row.ConsensusRightsDelay,
                    DelegateParametersActivationDelay = row.DelegateParametersActivationDelay,
                    TimeBetweenBlocks = row.TimeBetweenBlocks,
                    MinimalStake = row.MinimalStake,
                    MinimalFrozenStake = row.MinimalFrozenStake,
                    ProposalQuorum = row.ProposalQuorum / 100.0,
                    BallotQuorumMin = row.BallotQuorumMin / 100.0,
                    BallotQuorumMax = row.BallotQuorumMax / 100.0,
                    LBToggleThreshold = row.LBToggleThreshold,
                    ConsensusThreshold = row.ConsensusThreshold,
                    MaxDelegatedOverFrozenRatio = row.MaxDelegatedOverFrozenRatio,
                    MaxExternalOverOwnStakeRatio = row.MaxExternalOverOwnStakeRatio,
                    DenunciationPeriod = row.DenunciationPeriod,
                    SlashingDelay = row.SlashingDelay,
                    MinParticipationDenominator = row.MinParticipationDenominator,
                    MinParticipationNumerator = row.MinParticipationNumerator,
                    SmartRollupChallengeWindow = row.SmartRollupChallengeWindow,
                    SmartRollupCommitmentPeriod = row.SmartRollupCommitmentPeriod,
                    SmartRollupOriginationSize = row.SmartRollupOriginationSize,
                    SmartRollupStakeAmount = row.SmartRollupStakeAmount,
                    SmartRollupTimeoutPeriod = row.SmartRollupTimeoutPeriod,
                    DalNumberOfShards = row.NumberOfShards,
                    Dictator = row.Dictator
                },
                Extras = row.Extras
            };
        }

        public async Task<Protocol?> Get(string hash)
        {
            var sql = @"
                SELECT  *
                FROM    ""Protocols""
                WHERE   ""Hash"" = @hash::character(51)
                LIMIT   1";

            await using var db = await DataSource.OpenConnectionAsync();
            var row = await db.QueryFirstOrDefaultAsync(sql, new { hash });
            if (row == null) return null;

            return new Protocol
            {
                Code = row.Code,
                Hash = hash,
                Version = row.Version,
                FirstLevel = row.FirstLevel,
                FirstCycle = row.FirstCycle,
                FirstCycleLevel = row.FirstCycleLevel,
                LastLevel = row.LastLevel == -1 ? null : row.LastLevel,
                Constants = new ProtocolConstants
                {
                    RampUpCycles = row.RampUpCycles,
                    NoRewardCycles = row.NoRewardCycles,
                    BlockDeposit = row.BlockDeposit,
                    BlockReward = row.BlockReward1 == 0
                        ? [row.BlockReward0]
                        : [row.BlockReward0, row.BlockReward1],
                    BlocksPerCommitment = row.BlocksPerCommitment,
                    BlocksPerCycle = row.BlocksPerCycle,
                    BlocksPerSnapshot = row.BlocksPerSnapshot,
                    BlocksPerVoting = row.BlocksPerVoting,
                    ByteCost = row.ByteCost,
                    AttestationDeposit = row.AttestationDeposit,
                    AttestationReward = row.AttestationReward1 == 0
                        ? [row.AttestationReward0]
                        : [row.AttestationReward0, row.AttestationReward1],
                    AttestersPerBlock = row.AttestersPerBlock,
                    HardBlockGasLimit = row.HardBlockGasLimit,
                    HardOperationGasLimit = row.HardOperationGasLimit,
                    HardOperationStorageLimit = row.HardOperationStorageLimit,
                    OriginationSize = row.OriginationSize,
                    ConsensusRightsDelay = row.ConsensusRightsDelay,
                    DelegateParametersActivationDelay = row.DelegateParametersActivationDelay,
                    TimeBetweenBlocks = row.TimeBetweenBlocks,
                    MinimalStake = row.MinimalStake,
                    MinimalFrozenStake = row.MinimalFrozenStake,
                    ProposalQuorum = row.ProposalQuorum / 100.0,
                    BallotQuorumMin = row.BallotQuorumMin / 100.0,
                    BallotQuorumMax = row.BallotQuorumMax / 100.0,
                    LBToggleThreshold = row.LBToggleThreshold,
                    ConsensusThreshold = row.ConsensusThreshold,
                    MaxDelegatedOverFrozenRatio = row.MaxDelegatedOverFrozenRatio,
                    MaxExternalOverOwnStakeRatio = row.MaxExternalOverOwnStakeRatio,
                    DenunciationPeriod = row.DenunciationPeriod,
                    SlashingDelay = row.SlashingDelay,
                    MinParticipationDenominator = row.MinParticipationDenominator,
                    MinParticipationNumerator = row.MinParticipationNumerator,
                    SmartRollupChallengeWindow = row.SmartRollupChallengeWindow,
                    SmartRollupCommitmentPeriod = row.SmartRollupCommitmentPeriod,
                    SmartRollupOriginationSize = row.SmartRollupOriginationSize,
                    SmartRollupStakeAmount = row.SmartRollupStakeAmount,
                    SmartRollupTimeoutPeriod = row.SmartRollupTimeoutPeriod,
                    DalNumberOfShards = row.NumberOfShards,
                    Dictator = row.Dictator
                },
                Extras = row.Extras
            };
        }

        public async Task<IEnumerable<Protocol>> Get(SortParameter? sort, OffsetParameter? offset, int limit)
        {
            var sql = new SqlBuilder(@"SELECT * FROM ""Protocols""")
                .Take(sort ?? new(){ Asc = "code" }, offset, limit, x => x switch
                {
                    "firstLevel" => ("Code", "FirstLevel"),
                    "lastLevel" => ("Code", "LastLevel"),
                    _ => ("Code", "Code")
                });

            await using var db = await DataSource.OpenConnectionAsync();
            var rows = await db.QueryAsync(sql.Query, sql.Params);

            return rows.Select(row => new Protocol
            {
                Code = row.Code,
                Hash = row.Hash,
                Version = row.Version,
                FirstLevel = row.FirstLevel,
                FirstCycle = row.FirstCycle,
                FirstCycleLevel = row.FirstCycleLevel,
                LastLevel = row.LastLevel == -1 ? null : row.LastLevel,
                Constants = new ProtocolConstants
                {
                    RampUpCycles = row.RampUpCycles,
                    NoRewardCycles = row.NoRewardCycles,
                    BlockDeposit = row.BlockDeposit,
                    BlockReward = row.BlockReward1 == 0
                        ? [row.BlockReward0]
                        : [row.BlockReward0, row.BlockReward1],
                    BlocksPerCommitment = row.BlocksPerCommitment,
                    BlocksPerCycle = row.BlocksPerCycle,
                    BlocksPerSnapshot = row.BlocksPerSnapshot,
                    BlocksPerVoting = row.BlocksPerVoting,
                    ByteCost = row.ByteCost,
                    AttestationDeposit = row.AttestationDeposit,
                    AttestationReward = row.AttestationReward1 == 0
                        ? [row.AttestationReward0]
                        : [row.AttestationReward0, row.AttestationReward1],
                    AttestersPerBlock = row.AttestersPerBlock,
                    HardBlockGasLimit = row.HardBlockGasLimit,
                    HardOperationGasLimit = row.HardOperationGasLimit,
                    HardOperationStorageLimit = row.HardOperationStorageLimit,
                    OriginationSize = row.OriginationSize,
                    ConsensusRightsDelay = row.ConsensusRightsDelay,
                    DelegateParametersActivationDelay = row.DelegateParametersActivationDelay,
                    TimeBetweenBlocks = row.TimeBetweenBlocks,
                    MinimalStake = row.MinimalStake,
                    MinimalFrozenStake = row.MinimalFrozenStake,
                    ProposalQuorum = row.ProposalQuorum / 100.0,
                    BallotQuorumMin = row.BallotQuorumMin / 100.0,
                    BallotQuorumMax = row.BallotQuorumMax / 100.0,
                    LBToggleThreshold = row.LBToggleThreshold,
                    ConsensusThreshold = row.ConsensusThreshold,
                    MaxDelegatedOverFrozenRatio = row.MaxDelegatedOverFrozenRatio,
                    MaxExternalOverOwnStakeRatio = row.MaxExternalOverOwnStakeRatio,
                    DenunciationPeriod = row.DenunciationPeriod,
                    SlashingDelay = row.SlashingDelay,
                    MinParticipationDenominator = row.MinParticipationDenominator,
                    MinParticipationNumerator = row.MinParticipationNumerator,
                    SmartRollupChallengeWindow = row.SmartRollupChallengeWindow,
                    SmartRollupCommitmentPeriod = row.SmartRollupCommitmentPeriod,
                    SmartRollupOriginationSize = row.SmartRollupOriginationSize,
                    SmartRollupStakeAmount = row.SmartRollupStakeAmount,
                    SmartRollupTimeoutPeriod = row.SmartRollupTimeoutPeriod,
                    DalNumberOfShards = row.NumberOfShards,
                    Dictator = row.Dictator
                },
                Extras = row.Extras
            });
        }
    }
}
