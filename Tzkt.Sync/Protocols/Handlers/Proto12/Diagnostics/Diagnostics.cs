using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

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

            if(delegat.FrozenDepositLimit != null)
                Console.WriteLine("check");

            var a = remote.OptionalInt64("frozen_deposits_limit");
            if (remote.OptionalInt64("frozen_deposits_limit") != delegat.FrozenDepositLimit)
                throw new Exception($"Diagnostics failed: wrong frozen deposits limit {delegat.Address}");
            
            TestDelegatorsCount(remote, delegat);
        }

        protected override async Task TestParticipation(AppState state)
        {
            Console.WriteLine($"{nameof(TestParticipation)}:");

            var ind = 0;
            var bakers = await Db.Delegates.ToListAsync();
            var bakerCycles = await Db.BakerCycles.Where(x => x.Cycle == state.Cycle).ToDictionaryAsync(x => x.BakerId);
            var cycle = await Db.Cycles.SingleAsync(x => x.Index == state.Cycle);
            foreach (var baker in bakers)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{++ind} / {bakers.Count}");

                var remote = await Rpc.GetDelegateParticipationAsync(state.Level, baker.Address);
                if (bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                {
                    if ((long)bakerCycle.ExpectedEndorsements != remote.RequiredInt64("expected_cycle_activity"))
                        throw new Exception($"Invalid baker ExpectedEndorsements {baker.Address}");

                    if (bakerCycle.FutureEndorsementRewards != remote.RequiredInt64("expected_endorsing_rewards"))
                    {
                        var cycleStart = await Rpc.GetDelegateParticipationAsync(cycle.FirstLevel, baker.Address);
                        if (bakerCycle.FutureEndorsementRewards != cycleStart.RequiredInt64("expected_endorsing_rewards"))
                            throw new Exception($"Invalid baker FutureEndorsementRewards {baker.Address}");
                    }

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

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("done");
        }
    }
}
