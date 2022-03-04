using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class ProtoActivator : Proto11.ProtoActivator
    {
        public ProtoActivator(ProtocolHandler proto) : base(proto) { }

        protected override void SetParameters(Protocol protocol, JToken parameters)
        {
            base.SetParameters(protocol, parameters);
            protocol.BlocksPerSnapshot = parameters["blocks_per_stake_snapshot"]?.Value<int>() ?? 512;
            protocol.EndorsersPerBlock = parameters["consensus_committee_size"]?.Value<int>() ?? 7000;
            protocol.TokensPerRoll = parameters["tokens_per_roll"]?.Value<long>() ?? 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = parameters["baking_reward_fixed_portion"]?.Value<long>() ?? (totalReward / 4);
            protocol.BlockReward1 = parameters["baking_reward_bonus_per_slot"]?.Value<long>() ?? (totalReward / 4 / (protocol.EndorsersPerBlock / 3));
            protocol.EndorsementReward0 = parameters["endorsing_reward_per_slot"]?.Value<long>() ?? (totalReward / 2 / protocol.EndorsersPerBlock);
            protocol.EndorsementReward1 = 0;

            protocol.LBSunsetLevel = parameters["liquidity_baking_sunset_level"]?.Value<int>() ?? 3_063_809;

            protocol.ConsensusThreshold = parameters["consensus_threshold"]?.Value<int>() ?? 4667;
            protocol.MinParticipationNumerator = parameters["minimal_participation_ratio"]?["numerator"]?.Value<int>() ?? 2;
            protocol.MinParticipationDenominator = parameters["minimal_participation_ratio"]?["denominator"]?.Value<int>() ?? 3;
            protocol.MaxSlashingPeriod = parameters["max_slashing_period"]?.Value<int>() ?? 2;
            protocol.FrozenDepositsPercentage = parameters["frozen_deposits_percentage"]?.Value<int>() ?? 10;
            protocol.DoubleBakingPunishment = parameters["double_baking_punishment"]?.Value<long>() ?? 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["numerator"]?.Value<int>() ?? 1;
            protocol.DoubleEndorsingPunishmentDenominator = parameters["ratio_of_frozen_deposits_slashed_per_double_endorsement"]?["denominator"]?.Value<int>() ?? 2;
        }

        protected override void UpgradeParameters(Protocol protocol, Protocol prev)
        {
            protocol.EndorsersPerBlock = 7000;
            protocol.TokensPerRoll = 6_000_000_000;
            protocol.BlockDeposit = 0;
            protocol.EndorsementDeposit = 0;

            var totalReward = 80_000_000 / (60 / protocol.TimeBetweenBlocks);
            protocol.BlockReward0 = totalReward / 4;
            protocol.BlockReward1 = totalReward / 4 / (protocol.EndorsersPerBlock / 3);
            protocol.EndorsementReward0 = totalReward / 2 / protocol.EndorsersPerBlock;
            protocol.EndorsementReward1 = 0;

            if (protocol.LBSunsetLevel == 2_244_609)
                protocol.LBSunsetLevel = 3_063_809;

            protocol.ConsensusThreshold = 4667;
            protocol.MinParticipationNumerator = 2;
            protocol.MinParticipationDenominator = 3;
            protocol.MaxSlashingPeriod = 2;
            protocol.FrozenDepositsPercentage = 10;
            protocol.DoubleBakingPunishment = 640_000_000;
            protocol.DoubleEndorsingPunishmentNumerator = 1;
            protocol.DoubleEndorsingPunishmentDenominator = 2;
        }

        protected override async Task MigrateContext(AppState state)
        {
            var protocol = await Cache.Protocols.GetAsync(state.NextProtocol);
            var bakers = await MigrateBakers(protocol);
            await MigrateCycles(state, bakers);

        }

        public async Task PostActivation(AppState state)
        {
            #region make snapshot
            await Db.Database.ExecuteSqlRawAsync($@"
                INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                    SELECT {state.Level}, ""Balance"", ""Id"", ""DelegateId""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true");
            #endregion
        }

        public Task PreDeactivation(AppState state)
        {
            throw new NotImplementedException("Reverting Ithaca migration block is technically impossible");
        }

        protected override Task RevertContext(AppState state)
        {
            throw new NotImplementedException("Reverting Ithaca migration block is technically impossible");
        }

        async Task<List<Data.Models.Delegate>> MigrateBakers(Protocol protocol)
        {
            var bakers = await Db.Delegates.ToListAsync();
            foreach (var baker in bakers)
            {
                Cache.Accounts.Add(baker);
                baker.StakingBalance = baker.Balance + baker.DelegatedBalance;
                var activeStake = Math.Min(baker.StakingBalance, baker.Balance * 100 / protocol.FrozenDepositsPercentage);
                baker.FrozenDeposit = baker.Staked && activeStake >= protocol.TokensPerRoll
                    ? activeStake * protocol.FrozenDepositsPercentage / 100
                    : 0;
            }
            return bakers.Where(x => x.Staked).ToList();
        }

        async Task MigrateCycles(AppState state, List<Data.Models.Delegate> bakers)
        {
            var totalStaking = bakers.Sum(x => x.StakingBalance);
            var totalDelegated = bakers.Sum(x => x.DelegatedBalance);
            var totalDelegators = bakers.Sum(x => x.DelegatorsCount);
            var totalBakers = bakers.Count;

            var cycles = await Db.Cycles.Where(x => x.Index > state.Cycle).ToListAsync();
            foreach (var cycle in cycles)
            {
                cycle.SnapshotIndex = 0;
                cycle.SnapshotLevel = state.Level;
                cycle.TotalStaking = totalStaking;
                cycle.TotalDelegated = totalDelegated;
                cycle.TotalDelegators = totalDelegators;
                cycle.TotalBakers = totalBakers;
            }
        }
    }
}
