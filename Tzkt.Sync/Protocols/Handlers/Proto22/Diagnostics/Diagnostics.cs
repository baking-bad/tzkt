using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto22
{
    class Diagnostics : Proto21.Diagnostics
    {
        public Diagnostics(ProtocolHandler handler) : base(handler) { }

        protected override async Task TestDalParticipation(AppState state)
        {
            var bakers = Cache.Accounts.GetDelegates().ToList();
            var bakerCycles = Db.ChangeTracker.Entries()
                .Where(x => x.Entity is BakerCycle bc && bc.Cycle == state.Cycle)
                .Select(x => x.Entity as BakerCycle)
                .ToDictionary(x => x.BakerId);

            foreach (var baker in bakers)
            {
                var remote = await Rpc.GetDelegateDalParticipationAsync(state.Level, baker.Address);

                if (!bakerCycles.TryGetValue(baker.Id, out var bakerCycle))
                    bakerCycle = await Db.BakerCycles.FirstOrDefaultAsync(x => x.Cycle == state.Cycle && x.BakerId == baker.Id);

                if (bakerCycle != null)
                {
                    if (remote.RequiredInt64("expected_assigned_shards_per_slot") != bakerCycle.ExpectedDalShards)
                        throw new Exception($"Invalid baker ExpectedDalShards {baker.Address}");

                    if (remote.RequiredInt64("expected_dal_rewards") != bakerCycle.FutureDalAttestationRewards)
                    {
                        if (remote.RequiredInt64("expected_dal_rewards") != 0 || remote.RequiredBool("sufficient_dal_participation") && !remote.RequiredBool("denounced"))
                            throw new Exception($"Invalid baker FutureDalAttestationRewards {baker.Address}");
                    }
                }
                else
                {
                    if (remote.RequiredInt64("expected_assigned_shards_per_slot") != 0)
                        throw new Exception($"Invalid baker ExpectedDalShards {baker.Address}");

                    if (remote.RequiredInt64("expected_dal_rewards") != 0)
                        throw new Exception($"Invalid baker FutureDalAttestationRewards {baker.Address}");
                }
            }
        }
    }
}
