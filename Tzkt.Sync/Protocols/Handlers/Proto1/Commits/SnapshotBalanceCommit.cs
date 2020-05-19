using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public Block Block { get; private set; }

        SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public async override Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.Snapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                    SELECT {Block.Level}, (""Balance"" - COALESCE(""FrozenRewards"", 0)), ""Id"", ""DelegateId""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true");

                #region weird snapshots
                var weirdDelegators = (await Db.Contracts
                    .AsNoTracking()
                    .Include(x => x.WeirdDelegate)
                    .Where(x => 
                        x.DelegateId == null &&
                        x.WeirdDelegateId != null &&
                        x.WeirdDelegate.Type != AccountType.Delegate)
                    .ToListAsync())
                    .GroupBy(x => x.WeirdDelegateId);

                if (weirdDelegators.Any())
                {
                    var sql = @"
                        INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"") VALUES ";

                    var inserted = false;
                    foreach (var weirds in weirdDelegators)
                    {
                        if (weirds.Sum(x => x.Balance) < Block.Protocol.TokensPerRoll)
                            continue;

                        sql += $@"
                            ({Block.Level}, 0, {weirds.First().WeirdDelegate.Id}, NULL),";

                        foreach (var weird in weirds)
                        {
                            sql += $@"
                                ({Block.Level}, {weird.Balance}, {weird.Id}, {weird.WeirdDelegateId}),";
                        }

                        inserted = true;
                    }

                    if (inserted)
                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                }
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
            var commit = new SnapshotBalanceCommit(proto) { Block = block };
            await commit.Apply();
            return commit;
        }

        public static async Task<SnapshotBalanceCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new SnapshotBalanceCommit(proto) { Block = block };
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
