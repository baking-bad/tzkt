using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class StakerCyclesCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<(int, int), StakerCycle> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 4000;
            TargetCap = size?.TargetCap ?? 3000;
            Cached = new(SoftCap + 100);
        }
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            Cached.Clear();
        }

        public void Trim()
        {
            if (Cached.Count > SoftCap)
            {
                var toRemove = Cached.Values
                    .OrderBy(x => x.Id)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Remove(item);
            }
        }

        public void Add(StakerCycle sc)
        {
            Cached[(sc.StakerId, sc.Cycle)] = sc;
        }

        public void Remove(StakerCycle sc)
        {
            Cached.Remove((sc.StakerId, sc.Cycle));
        }

        public async Task<StakerCycle> GetOrCreateAsync(int stakerId, int cycle, Data.Models.Delegate baker)
        {
            if (!Cached.TryGetValue((stakerId, cycle), out var sc))
            {
                sc = await Db.StakerCycles.SingleOrDefaultAsync(x =>
                    x.StakerId == stakerId &&
                    x.Cycle == cycle);

                if (sc == null)
                {
                    sc = new StakerCycle
                    {
                        Id = 0,
                        Cycle = cycle,
                        StakerId = stakerId,
                        BakerId = baker.Id,
                        EdgeOfBakingOverStaking = baker.EdgeOfBakingOverStaking ?? 1_000_000_000
                    };
                    Db.StakerCycles.Add(sc);
                }

                Add(sc);
            }

            #region temp check
            if (sc.BakerId != baker.Id)
                throw new InvalidOperationException("StakerCycle's baker doesn't match");
            #endregion

            return sc;
        }
    }
}
