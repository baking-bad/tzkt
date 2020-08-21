using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class StatisticsCache
    {
        static Statistics CachedStatistics;

        readonly TzktContext Db;

        public StatisticsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedStatistics = null;
        }

        public void Add(Statistics stats)
        {
            CachedStatistics = stats;
        }

        public async Task<Statistics> GetAsync(int level)
        {
            if (CachedStatistics?.Level != level)
                CachedStatistics = await Db.Statistics.FirstAsync(x => x.Level == level);

            return CachedStatistics;
        }
    }
}
