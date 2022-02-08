using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    public class SchemasCache
    {
        public const int MaxItems = 4 * 4096; //TODO: set limits in app settings

        static readonly Dictionary<int, ContractScript> CachedById = new(MaxItems);

        readonly TzktContext Db;

        public SchemasCache(TzktContext db)
        {
            Db = db;
        }

        public void Reset()
        {
            CachedById.Clear();
        }

        public void Add(Contract contract, ContractScript schema)
        {
            CheckSpace();
            CachedById[contract.Id] = schema;
        }

        public async Task<ContractScript> GetAsync(Contract contract)
        {
            if (contract == null) return null;

            if (!CachedById.TryGetValue(contract.Id, out var item))
            {
                item = (await Db.Scripts.FirstOrDefaultAsync(x => x.ContractId == contract.Id && x.Current))?.Schema
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
                var oldest = CachedById.Keys
                    .OrderBy(x => x)
                    .Take(MaxItems / 4)
                    .ToList();

                foreach (var key in oldest)
                    CachedById.Remove(key);
            }
        }
    }
}
