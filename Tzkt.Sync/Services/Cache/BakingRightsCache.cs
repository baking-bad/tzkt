using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BakingRightsCache(TzktContext db)
    {
        static int CachedCycle = -1;
        static List<BakingRight>[] CachedRights = [];

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedCycle = -1;
            CachedRights = [];
        }

        public async Task<List<BakingRight>> GetAsync(int cycle, int level)
        {
            if (CachedCycle != cycle)
            {
                var rights = await Db.BakingRights
                    .AsNoTracking()
                    .Where(x => x.Cycle == cycle)
                    .OrderBy(x => x.Level)
                    .ToListAsync();

                var length = rights[^1].Level - rights[0].Level + 1;

                if (CachedRights.Length != length)
                    CachedRights = new List<BakingRight>[length];

                for (int i = 0; i < length; i++)
                    CachedRights[i] = new List<BakingRight>(40);

                foreach (var r in rights)
                    CachedRights[r.Level - rights[0].Level].Add(r);

                CachedCycle = cycle;
            }

            return CachedRights[level - CachedRights[0][0].Level];
        }
    }
}
