using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(Block block)
        {
            Block = block;
            Block.Protocol ??= await Cache.GetProtocolAsync(Block.ProtoCode);
        }

        public async override Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.Snapshot))
            {
                #region remove outdated
                var outdatedLevel = Block.Level - (Block.Protocol.PreservedCycles * 2 + 3) * Block.Protocol.BlocksPerCycle;
                if (outdatedLevel > 0)
                {
                    await Db.Database.ExecuteSqlRawAsync(outdatedLevel == Block.Protocol.BlocksPerSnapshot
                        ? $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" <= {outdatedLevel}"
                        : $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" = {outdatedLevel}");
                }
                #endregion

                #region make snapshot
                await Db.Database.ExecuteSqlRawAsync($@"
                INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                SELECT {Block.Level}, ""Balance"", ""Id"", ""DelegateId""
                FROM ""Accounts""
                WHERE ""Staked"" = true");
                #endregion
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
