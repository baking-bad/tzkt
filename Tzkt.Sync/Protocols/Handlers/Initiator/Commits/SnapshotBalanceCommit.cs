using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Initiator
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public Task Init(Block block)
        {
            Block = block;
            return Task.CompletedTask;
        }

        public async override Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.Snapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                SELECT {Block.Level}, ""Balance"", ""Id"", ""DelegateId""
                FROM ""Accounts""
                WHERE ""Staked"" = true");
            }
        }

        public override async Task Revert()
        {
            if (Block.Events.HasFlag(BlockEvents.Snapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""SnapshotBalances""
                WHERE ""Level"" = {Block.Level}");
            }
        }

        #region static
        public static async Task<SnapshotBalanceCommit> Apply(ProtocolHandler proto, Block block)
        {
            var commit = new SnapshotBalanceCommit(proto);
            await commit.Init(block);
            await commit.Apply();

            return commit;
        }

        public static async Task<SnapshotBalanceCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new SnapshotBalanceCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
