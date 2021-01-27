using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class ScriptsCache
    {
        public const int MaxItems = 256; //TODO: set limits in app settings

        static readonly Dictionary<int, Script> CachedById = new Dictionary<int, Script>(257);

        readonly TzktContext Db;

        public ScriptsCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
        }

        public void Add(Contract contract, Script script)
        {
            CheckSpace();
            CachedById[contract.Id] = script;
        }

        public async Task<Script> GetAsync(Contract contract)
        {
            if (contract == null) return null;

            if (!CachedById.TryGetValue(contract.Id, out var item))
            {
                item = await Db.Scripts.FirstOrDefaultAsync(x => x.ContractId == contract.Id)
                    ?? throw new Exception($"Script for contract #{contract.Id} doesn't exist");

                Add(contract, item);
            }

            return item;
        }

        public void Remove(Contract contract)
        {
            CachedById.Remove(contract.Id);
        }

        void CheckSpace()
        {
            if (CachedById.Count >= MaxItems)
            {
                var oldest = CachedById.Values
                    .Take(MaxItems / 4);

                foreach (var key in oldest.Select(x => x.ContractId).ToList())
                    CachedById.Remove(key);
            }
        }
    }
}
