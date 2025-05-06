using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class BakingRightsCache(TzktContext db)
    {
        #region static
        const int WindowLength = 512;
        const int LevelCap = 256;

        static int FirstLevel = -1;
        static int LastLevel = -1;
        static readonly List<List<BakingRight>> Cached = new(WindowLength);
        #endregion

        readonly TzktContext Db = db;

        public void Reset()
        {
            FirstLevel = -1;
            LastLevel = -1;
            Cached.Clear();
        }

        public async Task<List<BakingRight>> GetAsync(int level)
        {
            if (level > LastLevel)
            {
                await LoadRights(level - 2, level - 2 + WindowLength);
            }
            else if (level < FirstLevel)
            {
                await LoadRights(level + 2 - WindowLength, level + 2);
            }
            return Cached[level - FirstLevel];
        }

        async Task LoadRights(int from, int to)
        {
            var rights = await Db.BakingRights
                .AsNoTracking()
                .Where(x => x.Level >= from && x.Level < to)
                .OrderBy(x => x.Level)
                .ToListAsync();

            if (rights.Count == 0)
            {
                // should never happen
                throw new Exception($"No rights found for {from}..{to}");
            }

            FirstLevel = rights[0].Level;
            LastLevel = rights[^1].Level;

            Cached.Clear();
            for (int i = LastLevel - FirstLevel + 1; i != 0; i--)
                Cached.Add(new(LevelCap));

            foreach (var right in rights)
                Cached[right.Level - FirstLevel].Add(right);

        }
    }
}
