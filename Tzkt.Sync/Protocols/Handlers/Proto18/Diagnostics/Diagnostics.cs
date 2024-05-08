using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class Diagnostics : Proto16.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override bool CheckDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat)
        {
            return remote.RequiredInt64("delegated_balance") == delegat.DelegatedBalance + delegat.ExternalStakedBalance;
        }

        protected override async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            await base.TestDelegate(level, delegat, proto);

            var stakingBalance = await Rpc.GetCurrentStakingBalance(level, delegat.Address);

            if (stakingBalance.RequiredInt64("own_frozen") != delegat.OwnStakedBalance)
                throw new Exception($"Diagnostics failed: wrong own_frozen balance for {delegat.Address}");

            if (stakingBalance.RequiredInt64("staked_frozen") != delegat.ExternalStakedBalance)
                throw new Exception($"Diagnostics failed: wrong staked_frozen balance for {delegat.Address}");

            if (stakingBalance.RequiredInt64("delegated") != delegat.StakingBalance - delegat.OwnStakedBalance - delegat.ExternalStakedBalance)
                throw new Exception($"Diagnostics failed: wrong delegated balance for {delegat.Address}");

            if (level > proto.FirstLevel)
            {
                var stakingParameters = await Rpc.GetStakingParameters(level - 1, delegat.Address);

                if (stakingParameters.TryGetProperty("active", out var active))
                {
                    if (active.RequiredInt64("limit_of_staking_over_baking_millionth") != delegat.LimitOfStakingOverBaking)
                        throw new Exception($"Diagnostics failed: wrong limit_of_staking_over_baking_millionth for {delegat.Address}");

                    if (active.RequiredInt64("edge_of_baking_over_staking_billionth") != delegat.EdgeOfBakingOverStaking)
                        throw new Exception($"Diagnostics failed: wrong edge_of_baking_over_staking_billionth for {delegat.Address}");
                }
                else
                {
                    if (delegat.LimitOfStakingOverBaking != null || delegat.EdgeOfBakingOverStaking != null)
                        throw new Exception($"Diagnostics failed: wrong staking parameters for {delegat.Address}");
                }
            }
        }

        protected override async Task TestParticipation(AppState state)
        {
            var bakers = Cache.Accounts.GetDelegates().ToList();
            var bakerCycles = Db.ChangeTracker.Entries()
                .Where(x => x.Entity is BakerCycle bc && bc.Cycle == state.Cycle)
                .Select(x => x.Entity as BakerCycle)
                .ToDictionary(x => x.BakerId);

            foreach (var baker in bakers)
            {
                var remote = await Rpc.GetDelegateParticipationAsync(state.Level, baker.Address);
                
                if (!bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                    bakerCycle = await Db.BakerCycles.FirstOrDefaultAsync(x => x.Cycle == state.Cycle && x.BakerId == baker.Id);

                if (bakerCycle != null)
                {
                    if ((long)bakerCycle.ExpectedEndorsements != remote.RequiredInt64("expected_cycle_activity"))
                        throw new Exception($"Invalid baker ExpectedEndorsements {baker.Address}");

                    if (bakerCycle.FutureEndorsementRewards != remote.RequiredInt64("expected_attesting_rewards"))
                    {
                        if (remote.RequiredInt64("expected_attesting_rewards") != 0 || remote.RequiredInt32("expected_cycle_activity") - remote.RequiredInt32("missed_slots") >= remote.RequiredInt32("minimal_cycle_activity"))
                            throw new Exception($"Invalid baker FutureEndorsementRewards {baker.Address}");
                    }

                    if (bakerCycle.MissedEndorsements != remote.RequiredInt64("missed_slots"))
                    {
                        var proto = await Cache.Protocols.GetAsync(state.Protocol);
                        if (bakerCycle.Cycle != proto.FirstCycle && bakerCycle.BakingPower > 0)
                            throw new Exception($"Invalid baker MissedEndorsements {baker.Address}");
                    }
                }
                else
                {
                    if (remote.RequiredInt64("expected_cycle_activity") != 0)
                        throw new Exception($"Invalid baker ExpectedEndorsements {baker.Address}");

                    if (remote.RequiredInt64("expected_attesting_rewards") != 0)
                        throw new Exception($"Invalid baker FutureEndorsementRewards {baker.Address}");

                    if (remote.RequiredInt64("missed_slots") != 0)
                        throw new Exception($"Invalid baker MissedEndorsements {baker.Address}");
                }
            }
        }

        protected override async Task TestCycle(AppState state, Cycle cycle)
        {
            var level = Math.Min(state.Level, cycle.FirstLevel);
            var remote = await Rpc.GetCycleAsync(level, cycle.Index);

            if (remote.RequiredString("random_seed") != Hex.Convert(cycle.Seed))
                throw new Exception($"Invalid cycle {cycle.Index} seed {Hex.Convert(cycle.Seed)}");

            if (remote.RequiredArray("selected_stake_distribution").Count() != cycle.TotalBakers)
                throw new Exception($"Invalid cycle {cycle.Index} selected bakers {cycle.TotalBakers}");

            if (remote.Required("total_active_stake").RequiredInt64("frozen") + remote.Required("total_active_stake").RequiredInt64("delegated") != cycle.TotalBakingPower)
                throw new Exception($"Invalid cycle {cycle.Index} selected stake {cycle.TotalBakingPower}");
        }
    }
}
