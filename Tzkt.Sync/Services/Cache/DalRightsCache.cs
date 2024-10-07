using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class DalRightsCache
    {
        private static int CachedCycle = -1;
        private static int CachedFirstLevel = -1;
        private static Dictionary<int, DalRight>[] CachedRights = Array.Empty<Dictionary<int, DalRight>>();

        private TzktContext Db;

        public DalRightsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedCycle = -1;
            CachedFirstLevel = -1;
            CachedRights = Array.Empty<Dictionary<int, DalRight>>();
        }

        public async Task<Dictionary<int, DalRight>> GetAsync(int cycle, int level)
        {
            if (CachedCycle != cycle)
            {
                var rights = (await Db.DalRights
                    .AsNoTracking()
                    .Where(x => x.Cycle == cycle)
                    .OrderBy(x => x.Level)
                    .ToListAsync())
                    .GroupBy(x => x.Level);

                var firstLevel = rights.First().Key;
                var lastLevel = rights.Last().Key;
                var length = lastLevel - firstLevel + 1;

                CachedRights = new Dictionary<int, DalRight>[length];

                foreach (var r in rights)
                    CachedRights[r.Key - firstLevel] = r.ToDictionary(x => x.DelegateId);

                CachedCycle = cycle;
                CachedFirstLevel = firstLevel;
            }

            return CachedRights[level - CachedFirstLevel];
        }
    }
}
