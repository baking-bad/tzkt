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
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                    SELECT {block.Level}, (COALESCE(""StakingBalance"", ""Balance"") - COALESCE(""DelegatedBalance"", 0)), ""Id"", ""DelegateId""
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
                        if (weirds.Sum(x => x.Balance) < block.Protocol.TokensPerRoll)
                            continue;

                        sql += $@"
                            ({block.Level}, 0, {weirds.First().WeirdDelegate.Id}, NULL),";

                        foreach (var weird in weirds)
                        {
                            sql += $@"
                                ({block.Level}, {weird.Balance}, {weird.Id}, {weird.WeirdDelegateId}),";
                        }

                        inserted = true;
                    }

                    if (inserted)
                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                }
                #endregion
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE FROM ""SnapshotBalances""
                    WHERE ""Level"" = {block.Level}");
            }
        }
    }
}
