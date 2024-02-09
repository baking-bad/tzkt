using Microsoft.EntityFrameworkCore;
using Mvkt.Data;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Services.Cache
{
    public class StatisticsCache
    {
        static Statistics _Current;

        public Statistics Current => _Current;

        readonly MvktContext Db;

        public StatisticsCache(MvktContext db)
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
