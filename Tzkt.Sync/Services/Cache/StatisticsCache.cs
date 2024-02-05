using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class StatisticsCache
    {
        static Statistics _Current;

        public Statistics Current => _Current;

        readonly TzktContext Db;

        public StatisticsCache(TzktContext db)
        {
            Db = db;
        }

        public async Task ResetAsync()
        {
            _Current = await Db.Statistics.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        public void SetCurrent(Statistics stats)
        {
            _Current = stats;
        }
    }
}
