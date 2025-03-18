using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class StoragesCache(TzktContext db)
    {
        #region static
        static int SoftCap = 0;
        static int TargetCap = 0;
        static Dictionary<int, Storage> Cached = [];

        public static void Configure(CacheSize? size)
        {
            SoftCap = size?.SoftCap ?? 60_000;
            TargetCap = size?.TargetCap ?? 50_000;
            Cached = new(SoftCap + 1024);
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
                    .OrderBy(x => x.Level)
                    .Take(Cached.Count - TargetCap)
                    .ToList();

                foreach (var item in toRemove)
                    Cached.Remove(item.ContractId);
            }
        }

        public void Add(Contract contract, Storage storage)
        {
            Cached[contract.Id] = storage;
        }

        public void Remove(Contract contract)
        {
            Cached.Remove(contract.Id);
        }

        public async Task<Storage> GetAsync(Contract contract)
        {
            if (!Cached.TryGetValue(contract.Id, out var item))
            {
                item = await Db.Storages.FirstOrDefaultAsync(x => x.ContractId == contract.Id && x.Current)
                    ?? throw new Exception($"Storage for contract #{contract.Id} doesn't exist");

                Add(contract, item);
            }

            return item;
        }
    }
}
