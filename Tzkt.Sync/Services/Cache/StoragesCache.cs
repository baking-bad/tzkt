using Microsoft.EntityFrameworkCore;
using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class StoragesCache(TzktContext db)
    {
        const int MaxItems = 4 * 4096; //TODO: set limits in app settings
        static readonly Dictionary<int, Storage> CachedByContractId = new(MaxItems);

        readonly TzktContext Db = db;

        public void Reset()
        {
            CachedByContractId.Clear();
        }

        public void Add(Contract contract, Storage storage)
        {
            CheckSpace();
            CachedByContractId[contract.Id] = storage;
        }

        public async Task<Storage> GetKnownAsync(Contract contract)
        {
            if (!CachedByContractId.TryGetValue(contract.Id, out var item))
            {
                item = await Db.Storages.FirstOrDefaultAsync(x => x.ContractId == contract.Id && x.Current)
                    ?? throw new Exception($"Storage for contract #{contract.Id} doesn't exist");

                Add(contract, item);
            }

            return item;
        }

        public async Task<Storage?> GetAsync(Contract? contract)
        {
            if (contract == null) return null;

            if (!CachedByContractId.TryGetValue(contract.Id, out var item))
            {
                item = await Db.Storages.FirstOrDefaultAsync(x => x.ContractId == contract.Id && x.Current)
                    ?? throw new Exception($"Storage for contract #{contract.Id} doesn't exist");

                Add(contract, item);
            }

            return item;
        }

        public void Remove(Contract contract)
        {
            CachedByContractId.Remove(contract.Id);
        }

        void CheckSpace()
        {
            if (CachedByContractId.Count >= MaxItems)
            {
                var oldest = CachedByContractId.Values
                    .OrderBy(x => x.Level)
                    .Take(MaxItems / 4)
                    .Select(x => x.ContractId)
                    .ToList();

                foreach (var key in oldest)
                    CachedByContractId.Remove(key);
            }
        }
    }
}
