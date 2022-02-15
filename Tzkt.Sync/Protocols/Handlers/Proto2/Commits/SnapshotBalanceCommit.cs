using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement rawBlock)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                #region remove outdated
                var delete = string.Empty;
                var outdatedLevel = block.Level - (block.Protocol.PreservedCycles + 3) * block.Protocol.BlocksPerCycle;
                if (outdatedLevel > 0)
                {
                    delete += block.Cycle <= block.Protocol.FirstCycle + block.Protocol.PreservedCycles + 4
                        ? $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" <= {outdatedLevel};"
                        : $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" = {outdatedLevel};";
                }
                #endregion

                #region make snapshot
                await Db.Database.ExecuteSqlRawAsync($@"
                    {delete}
                    INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                    SELECT {block.Level}, (COALESCE(""StakingBalance"", ""Balance"") - COALESCE(""DelegatedBalance"", 0)), ""Id"", ""DelegateId""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true;");
                #endregion

                #region ignore just deactivated
                if (block.Events.HasFlag(BlockEvents.Deactivations))
                {
                    var deactivated = await Db.Delegates
                        .AsNoTracking()
                        .Include(x => x.DelegatedAccounts)
                        .Where(x => x.DeactivationLevel == block.Level)
                        .ToListAsync();

                    if (deactivated.Any())
                    {
                        var sql = @"
                            INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"") VALUES ";

                        foreach (var baker in deactivated)
                        {
                            sql += $@"
                                ({block.Level}, {baker.StakingBalance - baker.DelegatedBalance}, {baker.Id}, NULL),";

                            foreach (var delegator in baker.DelegatedAccounts)
                                sql += $@"
                                    ({block.Level}, {delegator.Balance}, {delegator.Id}, {delegator.DelegateId}),";
                        }

                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                    }
                }
                #endregion

                #region revert unfrozen rewards
                if (block.Events.HasFlag(BlockEvents.CycleEnd))
                {
                    var values = string.Empty;
                    foreach (var rewardUpdates in GetBalanceUpdates(rawBlock)
                        .Where(x => x.RequiredString("kind")[0] == 'f' &&
                                    x.RequiredString("category")[0] == 'r' &&
                                    x.RequiredInt64("change") < 0 &&
                                    GetFreezerCycle(x) != block.Cycle)
                        .Select(x => (x.RequiredString("delegate"), x.RequiredInt64("change")))
                        .GroupBy(x => x.Item1))
                        values += $@"
                                ({Cache.Accounts.GetDelegate(rewardUpdates.Key).Id}, {rewardUpdates.Sum(x => -x.Item2)}::bigint),";

                    if (values.Length > 0)
                    {
                        await Db.Database.ExecuteSqlRawAsync($@"
                            UPDATE  ""SnapshotBalances"" as sb
                            SET     ""Balance"" = ""Balance"" - rw.value
                            FROM    (VALUES {values[..^1]}) as rw(delegate, value)
                            WHERE   sb.""Level"" = {block.Level}
                            AND     sb.""AccountId"" = rw.delegate;");
                    }
                }
                #endregion
            }
        }

        public async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.BalanceSnapshot))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE FROM ""SnapshotBalances""
                    WHERE ""Level"" = {block.Level}");
            }
        }

        protected virtual int GetFreezerCycle(JsonElement el) => el.RequiredInt32("level");

        protected virtual IEnumerable<JsonElement> GetBalanceUpdates(JsonElement rawBlock)
        {
            return rawBlock
                .GetProperty("metadata")
                .GetProperty("balance_updates")
                .EnumerateArray();
        }
    }
}
