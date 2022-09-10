using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto10
{
    class SubsidyCommit : ProtocolCommit
    {
        public SubsidyCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, JsonElement content)
        {
            var balanceUpdate = content.RequiredArray("balance_updates").EnumerateArray()
                .First(x => x.RequiredString("kind") == "contract");
            var contract = await Cache.Accounts.GetAsync(balanceUpdate.RequiredString("contract")) as Contract;
            Db.TryAttach(contract);
            var op = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Account = contract,
                BalanceChange = balanceUpdate.RequiredInt64("change"),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                Kind = MigrationKind.Subsidy,
            };
            Db.MigrationOps.Add(op);
            Cache.AppState.Get().MigrationOpsCount++;

            contract.MigrationsCount++;
            contract.Balance += op.BalanceChange;

            block.Events |= BlockEvents.SmartContracts;
            block.Operations |= Operations.Migrations;
            
            var schema = await Cache.Schemas.GetAsync(contract);
            var currStorage = await Cache.Storages.GetAsync(contract);

            Db.TryAttach(currStorage);
            currStorage.Current = false;

            var newStorageMicheline = schema.OptimizeStorage(Micheline.FromJson(content.Required("storage")), false);
            var newStorageBytes = newStorageMicheline.ToBytes();
            var newStorage = new Storage
            {
                Id = Cache.AppState.NextStorageId(),
                Level = op.Level,
                ContractId = contract.Id,
                MigrationId = op.Id,
                RawValue = newStorageBytes,
                JsonValue = schema.HumanizeStorage(newStorageMicheline),
                Current = true,
            };

            Db.Storages.Add(newStorage);
            Cache.Storages.Add(contract, newStorage);

            op.Storage = newStorage;
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Migrations == null)
                return;

            foreach (var op in block.Migrations.Where(x => x.Kind == MigrationKind.Subsidy))
            {
                var contract = await Cache.Accounts.GetAsync(op.AccountId) as Contract;
                Db.TryAttach(contract);
                contract.Balance -= op.BalanceChange;
                contract.MigrationsCount--;

                Cache.AppState.Get().MigrationOpsCount--;
                Cache.AppState.ReleaseOperationId();
                Db.MigrationOps.Remove(op);
                
                var storage = await Cache.Storages.GetAsync(contract);
                if (storage.MigrationId == op.Id)
                {
                    var prevStorage = await Db.Storages
                        .Where(x => x.ContractId == contract.Id && x.Id < storage.Id)
                        .OrderByDescending(x => x.Id)
                        .FirstAsync();

                    prevStorage.Current = true;
                    Cache.Storages.Add(contract, prevStorage);

                    Db.Storages.Remove(storage);
                    Cache.AppState.ReleaseStorageId();
                }
            }
        }
    }
}
