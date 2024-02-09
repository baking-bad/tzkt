using Microsoft.EntityFrameworkCore;
using Netmavryk.Contracts;
using Netmavryk.Encoding;
using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

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

        public void Add(SmartRollup smartRollup, ContractScript schema)
        {
            CheckSpace();
            CachedById[smartRollup.Id] = schema;
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

        public async Task<ContractScript> GetAsync(SmartRollup smartRollup)
        {
            if (smartRollup == null) return null;

            if (!CachedById.TryGetValue(smartRollup.Id, out var item))
            {
                var bytes = (await Db.SmartRollupOriginateOps.SingleOrDefaultAsync(x => x.SmartRollupId == smartRollup.Id && x.Status == OperationStatus.Applied))?.ParameterType
                    ?? throw new Exception($"Origination of smart rollup #{smartRollup.Id} doesn't exist");
                var parameter = new MichelinePrim
                {
                    Prim = PrimType.parameter,
                    Args = new(1) { Micheline.FromBytes(bytes) }
                };
                var storage = new MichelinePrim
                {
                    Prim = PrimType.storage,
                    Args = new(1) { new MichelinePrim { Prim = PrimType.never } }
                };
                item = new ContractScript(parameter, storage);
                Add(smartRollup, item);
            }

            return item;
        }

        public void Remove(Contract contract)
        {
            CachedById.Remove(contract.Id);
        }

        public void Remove(SmartRollup smartRollup)
        {
            CachedById.Remove(smartRollup.Id);
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
