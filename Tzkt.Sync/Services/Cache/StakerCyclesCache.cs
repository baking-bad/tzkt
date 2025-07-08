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
        static Dictionary<(int, int, int), StakerCycle> Cached = [];

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
            Cached[(sc.Cycle, sc.BakerId, sc.StakerId)] = sc;
        }

        public void Remove(StakerCycle sc)
        {
            Cached.Remove((sc.Cycle, sc.BakerId, sc.StakerId));
        }

        public async Task<StakerCycle> GetOrCreateAsync(int cycle, int bakerId, int stakerId)
        {
            if (!Cached.TryGetValue((cycle, bakerId, stakerId), out var sc))
            {
                sc = await Db.StakerCycles.SingleOrDefaultAsync(x =>
                    x.Cycle == cycle &&
                    x.BakerId == bakerId &&
                    x.StakerId == stakerId);

                if (sc == null)
                {
                    sc = new StakerCycle
                    {
                        Id = 0,
                        Cycle = cycle,
                        BakerId = bakerId,
                        StakerId = stakerId,
                    };
                    Db.StakerCycles.Add(sc);
                }

                Add(sc);
            }

            return sc;
        }
    }
}
