using System.Collections;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class BlockCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public Block Block { get; private set; } = null!;

        public virtual async Task Apply(JsonElement rawBlock)
        {
            var hash = rawBlock.RequiredString("hash");
            var header = rawBlock.Required("header");
            var level = header.RequiredInt32("level");
            var timestamp = header.RequiredDateTime("timestamp");
            var metadata = rawBlock.Required("metadata");

            var baker = await GetOrCreateBaker(metadata.OptionalString("baker"), level, timestamp);
            var protocol = await Cache.Protocols.GetAsync(rawBlock.RequiredString("protocol"));

            Block = new Block
            {
                Id = Cache.AppState.NextOperationId(),
                Hash = hash,
                Level = level,
                Cycle = 0,
                ProtoCode = protocol.Code,
                Timestamp = timestamp,
                ProposerId = baker?.Id,
                ProducerId = baker?.Id,
                Operations = Context.MigrationOps.Count != 0
                    ? Operations.Migrations
                    : Operations.None,
            };

            if (baker != null)
            {
                Db.TryAttach(baker);
                baker.BlocksCount++;
            }

            Context.Block = Block;
            Context.Protocol = protocol;

            Cache.AppState.Get().BlocksCount++;

            Db.Blocks.Add(Block);
            Cache.Blocks.Add(Block);
        }

        public virtual void Revert(Block block)
        {
            var baker = Cache.Accounts.GetDelegate(block.ProposerId);

            if (baker != null)
            {
                Db.TryAttach(baker);
                baker.BlocksCount--;
            }

            Cache.AppState.Get().BlocksCount--;

            Db.Blocks.Remove(block);
            Cache.Blocks.Remove(block);
            Cache.AppState.ReleaseOperationId();
        }

        public async Task<Data.Models.Delegate?> GetOrCreateBaker(string? address, int level, DateTime timestamp)
        {
            if (address is not string _address)
                return null;

            if (Cache.Accounts.DelegateExists(_address))
                return Cache.Accounts.GetExistingDelegate(_address);

            if (await Cache.Accounts.ExistsAsync(_address))
                throw new NotImplementedException();

            var baker = new Data.Models.Delegate
            {
                Id = Cache.AppState.NextAccountId(),
                Address = _address,
                Type = AccountType.Delegate,
                FirstLevel = level,
                LastLevel = level,
                Staked = true,
                ActivationLevel = level,
                DeactivationLevel = int.MaxValue,
            };

            Db.Accounts.Add(baker);
            Cache.Accounts.Add(baker);
            Cache.Statistics.Current.TotalBakers++;

            var migration = new MigrationOperation
            {
                Id = Cache.AppState.NextOperationId(),
                Level = level,
                Timestamp = timestamp,
                AccountId = baker.Id,
                Kind = MigrationKind.ActivateDelegate,
                BalanceChange = baker.Balance,
            };

            baker.MigrationsCount++;

            Cache.AppState.Get().MigrationOpsCount++;

            Cache.Statistics.Current.TotalBootstrapped += migration.BalanceChange;

            Db.MigrationOps.Add(migration);
            Context.MigrationOps.Add(migration);

            return baker;
        }
    }
}
