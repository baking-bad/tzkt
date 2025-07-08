using System.Text.Json;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Diagnostics(ProtocolHandler handler) : Proto5.Diagnostics(handler)
    {
        protected override async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            var remote = await Rpc.GetDelegateAsync(level, delegat.Address);

            if (!CheckFullBalance(remote, delegat))
                throw new Exception($"Diagnostics failed: wrong balance {delegat.Address}");
            
            if (!CheckStakingBalance(remote, delegat))
                throw new Exception($"Diagnostics failed: wrong staking balance {delegat.Address}");

            if (!CheckDelegatedBalance(remote, delegat))
                throw new Exception($"Diagnostics failed: wrong delegated balance {delegat.Address}");

            if (!CheckMinDelegatedBalance(remote, delegat))
                throw new Exception($"Diagnostics failed: wrong min delegated balance {delegat.Address}");

            if (remote.RequiredBool("deactivated") != !delegat.Staked)
                throw new Exception($"Diagnostics failed: wrong deactivation state {delegat.Address}");

            var deactivationCycle = (delegat.DeactivationLevel - 1) >= proto.FirstLevel
                ? proto.GetCycle(delegat.DeactivationLevel - 1)
                : (await Cache.Blocks.GetAsync(delegat.DeactivationLevel - 1)).Cycle;

            if (remote.RequiredInt32("grace_period") != deactivationCycle)
                throw new Exception($"Diagnostics failed: wrong grace period {delegat.Address}");

            if (!CheckFrozenDepositLimit(remote, delegat))
                throw new Exception($"Diagnostics failed: wrong frozen deposits limit {delegat.Address}");
            
            TestDelegatorsCount(remote, delegat);
        }

        protected override async Task TestParticipation(AppState state)
        {
            var bakers = Cache.Accounts.GetDelegates().ToList();
            var bakerCycles = Db.ChangeTracker.Entries()
                .Where(x => x.Entity is BakerCycle bc && bc.Cycle == state.Cycle)
                .Select(x => (x.Entity as BakerCycle)!)
                .ToDictionary(x => x.BakerId);

            foreach (var baker in bakers)
            {
                var remote = await Rpc.GetDelegateParticipationAsync(state.Level, baker.Address);
                
                if (bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                {
                    if ((long)bakerCycle.ExpectedAttestations != remote.RequiredInt64("expected_cycle_activity"))
                        throw new Exception($"Invalid baker ExpectedAttestations {baker.Address}");

                    if (bakerCycle.FutureAttestationRewards != remote.RequiredInt64("expected_endorsing_rewards"))
                        throw new Exception($"Invalid baker FutureAttestationRewards {baker.Address}");

                    if (bakerCycle.MissedAttestations != remote.RequiredInt64("missed_slots"))
                    {
                        var proto = await Cache.Protocols.GetAsync(state.Protocol);
                        if (bakerCycle.Cycle != proto.FirstCycle)
                            throw new Exception($"Invalid baker MissedAttestations {baker.Address}");
                    }
                }
                else
                {
                    if (remote.RequiredInt64("expected_cycle_activity") != 0)
                        throw new Exception($"Invalid baker ExpectedAttestations {baker.Address}");

                    if (remote.RequiredInt64("expected_endorsing_rewards") != 0)
                        throw new Exception($"Invalid baker FutureAttestationRewards {baker.Address}");

                    if (remote.RequiredInt64("missed_slots") != 0)
                        throw new Exception($"Invalid baker MissedAttestations {baker.Address}");
                }
            }
        }
        
        protected override async Task TestCycle(AppState state, Cycle cycle)
        {
            var level = Math.Min(state.Level, cycle.FirstLevel);
            var remote = await Rpc.GetCycleAsync(level, cycle.Index);
                
            if (remote.RequiredString("random_seed") != Hex.Convert(cycle.Seed))
                throw new Exception($"Invalid cycle {cycle.Index} seed {Hex.Convert(cycle.Seed)}");

            if (remote.RequiredInt64("total_active_stake") != cycle.TotalBakingPower)
                throw new Exception($"Invalid cycle {cycle.Index} selected stake {cycle.TotalBakingPower}");

            if (remote.RequiredArray("selected_stake_distribution").Count() != cycle.TotalBakers)
                throw new Exception($"Invalid cycle {cycle.Index} selected bakers {cycle.TotalBakers}");
        }

        protected virtual bool CheckFullBalance(JsonElement remote, Data.Models.Delegate delegat) =>
            remote.RequiredInt64("full_balance") == delegat.Balance;

        protected virtual bool CheckStakingBalance(JsonElement remote, Data.Models.Delegate delegat) =>
            remote.RequiredInt64("staking_balance") == delegat.StakingBalance;

        protected virtual bool CheckDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat) =>
            remote.RequiredInt64("delegated_balance") == delegat.DelegatedBalance + delegat.RollupBonds;

        protected virtual bool CheckMinDelegatedBalance(JsonElement remote, Data.Models.Delegate delegat) => true;

        protected virtual bool CheckFrozenDepositLimit(JsonElement remote, Data.Models.Delegate delegat) =>
            remote.OptionalInt64("frozen_deposits_limit") == delegat.FrozenDepositLimit;
    }
}
