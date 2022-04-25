using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Sync.Utils;

namespace Tzkt.Sync.Protocols.Proto12
{
    class Diagnostics : Proto5.Diagnostics
    {
        protected readonly IRpc Rpc;

        public Diagnostics(ProtocolHandler handler) : base(handler)
        {
            Rpc = handler.Rpc;
        }

        protected override async Task TestDelegate(int level, Data.Models.Delegate delegat, Protocol proto)
        {
            var remote = await Rpc.GetDelegateAsync(level, delegat.Address);

            if (remote.RequiredInt64("full_balance") != delegat.Balance)
                throw new Exception($"Diagnostics failed: wrong balance {delegat.Address}");

            if (remote.RequiredInt64("current_frozen_deposits") != delegat.FrozenDeposit)
                throw new Exception($"Diagnostics failed: wrong frozen deposits {delegat.Address}");

            if (remote.RequiredInt64("staking_balance") != delegat.StakingBalance)
                throw new Exception($"Diagnostics failed: wrong staking balance {delegat.Address}");

            if (remote.RequiredInt64("delegated_balance") != delegat.DelegatedBalance)
                throw new Exception($"Diagnostics failed: wrong delegated balance {delegat.Address}");

            if (remote.RequiredBool("deactivated") != !delegat.Staked)
                throw new Exception($"Diagnostics failed: wrong deactivation state {delegat.Address}");

            var deactivationCycle = (delegat.DeactivationLevel - 1) >= proto.FirstLevel
                ? proto.GetCycle(delegat.DeactivationLevel - 1)
                : (await Cache.Blocks.GetAsync(delegat.DeactivationLevel - 1)).Cycle;

            if (remote.RequiredInt32("grace_period") != deactivationCycle)
                throw new Exception($"Diagnostics failed: wrong grace period {delegat.Address}");

            if (remote.OptionalInt64("frozen_deposits_limit") != delegat.FrozenDepositLimit)
                throw new Exception($"Diagnostics failed: wrong frozen deposits limit {delegat.Address}");
            
            TestDelegatorsCount(remote, delegat);
        }

        protected override async Task TestParticipation(AppState state)
        {
            var bakers = Cache.Accounts.GetDelegates().ToList();
            var bakerCycles = await Cache.BakerCycles.GetAsync(state.Cycle);

            foreach (var baker in bakers)
            {
                var remote = await Rpc.GetDelegateParticipationAsync(state.Level, baker.Address);
                
                if (bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                {
                    if ((long)bakerCycle.ExpectedEndorsements != remote.RequiredInt64("expected_cycle_activity"))
                        throw new Exception($"Invalid baker ExpectedEndorsements {baker.Address}");

                    if (bakerCycle.FutureEndorsementRewards != remote.RequiredInt64("expected_endorsing_rewards"))
                        throw new Exception($"Invalid baker FutureEndorsementRewards {baker.Address}");

                    if (bakerCycle.MissedEndorsements != remote.RequiredInt64("missed_slots"))
                        throw new Exception($"Invalid baker MissedEndorsements {baker.Address}");
                }
                else
                {
                    if (0 != remote.RequiredInt64("expected_cycle_activity"))
                        throw new Exception($"Invalid baker ExpectedEndorsements {baker.Address}");

                    if (0L != remote.RequiredInt64("expected_endorsing_rewards"))
                        throw new Exception($"Invalid baker FutureEndorsementRewards {baker.Address}");

                    if (0 != remote.RequiredInt64("missed_slots"))
                        throw new Exception($"Invalid baker MissedEndorsements {baker.Address}");
                }
            }
        }
        
        protected override async Task TestCycles(AppState state, Cycle cycle)
        {
            var level = Math.Min(state.Level, cycle.FirstLevel);
            var remote = await Rpc.GetRawCycleAsync(level, cycle.Index);
                
            if (remote.RequiredString("random_seed") != Hex.Convert(cycle.Seed))
                throw new Exception($"Invalid cycle {cycle.Index} seed {Hex.Convert(cycle.Seed)}");

            if (cycle.Index == 1) return;

            if (remote.RequiredInt64("total_active_stake") != cycle.SelectedStake)
                throw new Exception($"Invalid cycle {cycle.Index} selected stake {cycle.SelectedStake}");

            if (remote.RequiredArray("selected_stake_distribution").Count() != cycle.SelectedBakers)
                throw new Exception($"Invalid cycle {cycle.Index} selected bakers {cycle.SelectedBakers}");
        }
    }
}
