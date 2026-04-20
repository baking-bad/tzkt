using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto24
{
    public class ProtoMigrator(ProtocolHandler proto) : IMigrator
    {
        protected readonly ProtocolHandler Proto = proto;
        protected readonly IRpc Rpc = proto.Rpc;
        protected readonly TzktContext Db = proto.Db;
        protected readonly CacheService Cache = proto.Cache;
        protected readonly BlockContext Context = proto.Context;
        protected readonly ILogger Logger = proto.Logger;

        public async Task MigrateContext(AppState state)
        {
            #region protocol
            var prev = await Cache.Protocols.GetAsync(state.Protocol);
            Db.TryAttach(prev);
            prev.LastLevel = state.Level;

            var protocol = new Protocol
            {
                Id = 0,
                Code = prev.Code + 1,
                Hash = state.NextProtocol,
                Version = Proto.VersionNumber,
                FirstLevel = state.Level + 1,
                LastLevel = -1,
                FirstCycle = prev.FirstCycle,
                FirstCycleLevel = prev.FirstCycleLevel,
                RampUpCycles = prev.RampUpCycles,
                NoRewardCycles = prev.NoRewardCycles,
                BlockDeposit = prev.BlockDeposit,
                BlockReward0 = prev.BlockReward0,
                BlockReward1 = prev.BlockReward1,
                BlocksPerCommitment = prev.BlocksPerCommitment,
                BlocksPerCycle = prev.BlocksPerCycle,
                BlocksPerSnapshot = prev.BlocksPerSnapshot,
                BlocksPerVoting = prev.BlocksPerVoting,
                ByteCost = prev.ByteCost,
                AttestationDeposit = prev.AttestationDeposit,
                AttestationReward0 = prev.AttestationReward0,
                AttestationReward1 = prev.AttestationReward1,
                AttestersPerBlock = prev.AttestersPerBlock,
                HardBlockGasLimit = prev.HardBlockGasLimit,
                HardOperationGasLimit = prev.HardOperationGasLimit,
                HardOperationStorageLimit = prev.HardOperationStorageLimit,
                OriginationSize = prev.OriginationSize,
                ConsensusRightsDelay = prev.ConsensusRightsDelay,
                ToleratedInactivityPeriod = prev.ToleratedInactivityPeriod,
                TimeBetweenBlocks = prev.TimeBetweenBlocks,
                MinimalStake = prev.MinimalStake,
                BallotQuorumMin = prev.BallotQuorumMin,
                BallotQuorumMax = prev.BallotQuorumMax,
                ProposalQuorum = prev.ProposalQuorum,
                LBToggleThreshold = prev.LBToggleThreshold,
                ConsensusThreshold = prev.ConsensusThreshold,
                MaxDelegatedOverFrozenRatio = prev.MaxDelegatedOverFrozenRatio,
                MaxExternalOverOwnStakeRatio = prev.MaxExternalOverOwnStakeRatio,
                DoubleBakingSlashedPercentage = prev.DoubleBakingSlashedPercentage,
                DoubleConsensusSlashedPercentage = prev.DoubleConsensusSlashedPercentage,
                MinimalFrozenStake = prev.MinimalFrozenStake,
                StakePowerMultiplier = prev.StakePowerMultiplier,
                MaxBakingReward = prev.MaxBakingReward,
                MaxAttestationReward = prev.MaxAttestationReward,
                DenunciationPeriod = prev.DenunciationPeriod,
                SlashingDelay = prev.SlashingDelay,
                MinParticipationDenominator = prev.MinParticipationDenominator,
                MinParticipationNumerator = prev.MinParticipationNumerator,
                Dictator = prev.Dictator,
                SmartRollupChallengeWindow = prev.SmartRollupChallengeWindow,
                SmartRollupCommitmentPeriod = prev.SmartRollupCommitmentPeriod,
                SmartRollupOriginationSize = prev.SmartRollupOriginationSize,
                SmartRollupStakeAmount = prev.SmartRollupStakeAmount,
                SmartRollupTimeoutPeriod = prev.SmartRollupTimeoutPeriod,
                DelegateParametersActivationDelay = prev.DelegateParametersActivationDelay,
                NumberOfShards = prev.NumberOfShards
            };

            state.ProtocolsCount++;

            Db.Protocols.Add(protocol);
            Cache.Protocols.Add(protocol);
            Context.Protocol = protocol;
            #endregion
        }

        public async Task RevertContext(AppState state)
        {
            #region protocol
            var current = await Cache.Protocols.GetAsync(state.NextProtocol);
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Protocols"
                WHERE "Code" = {0}
                """, current.Code);

            state.ProtocolsCount--;

            var prev = await Cache.Protocols.GetAsync(state.Protocol);
            Db.TryAttach(prev);
            prev.LastLevel = -1;

            await Cache.Protocols.ResetAsync();
            #endregion
        }
    }
}
