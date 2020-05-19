using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class SnapshotBalanceCommit : ProtocolCommit
    {
        public RawBlock RawBlock { get; private set; }
        public Block Block { get; private set; }

        SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        public async override Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.Snapshot))
            {
                #region remove outdated
                var delete = string.Empty;
                var outdatedLevel = Block.Level - (Block.Protocol.PreservedCycles * 2 + 3) * Block.Protocol.BlocksPerCycle;
                if (outdatedLevel > 0)
                {
                    delete += outdatedLevel == Block.Protocol.BlocksPerSnapshot
                        ? $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" <= {outdatedLevel};"
                        : $@"DELETE FROM ""SnapshotBalances"" WHERE ""Level"" = {outdatedLevel};";
                }
                #endregion

                #region make snapshot
                await Db.Database.ExecuteSqlRawAsync($@"
                    {delete}
                    INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"")
                    SELECT {Block.Level}, (""Balance"" - COALESCE(""FrozenRewards"", 0)), ""Id"", ""DelegateId""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true;");
                #endregion

                #region ignore just deactivated
                if (Block.Events.HasFlag(BlockEvents.Deactivations))
                {
                    var deactivated = await Db.Delegates
                        .AsNoTracking()
                        .Include(x => x.DelegatedAccounts)
                        .Where(x => x.DeactivationLevel == Block.Level)
                        .ToListAsync();

                    if (deactivated.Any())
                    {
                        var sql = @"
                            INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"") VALUES ";

                        foreach (var baker in deactivated)
                        {
                            sql += $@"
                                ({Block.Level}, {baker.Balance - baker.FrozenRewards}, {baker.Id}, NULL),";

                            foreach (var delegator in baker.DelegatedAccounts)
                                sql += $@"
                                    ({Block.Level}, {delegator.Balance}, {delegator.Id}, {delegator.DelegateId}),";
                        }

                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                    }
                }
                #endregion

                #region revert unfrozen rewards
                if (Block.Events.HasFlag(BlockEvents.CycleEnd))
                {
                    var values = string.Empty;

                    foreach (var rewardUpdates in RawBlock.Metadata.BalanceUpdates
                        .Where(x => x is RewardsUpdate update && update.Level != RawBlock.Metadata.LevelInfo.Cycle)
                        .Select(x => x as RewardsUpdate)
                        .GroupBy(x => x.Delegate))
                        values += $@"
                                ({Cache.Accounts.GetDelegate(rewardUpdates.Key).Id}, {rewardUpdates.Sum(x => -x.Change)}::bigint),";

                    if (values.Length > 0)
                    {
                        await Db.Database.ExecuteSqlRawAsync($@"
                            UPDATE  ""SnapshotBalances"" as sb
                            SET     ""Balance"" = ""Balance"" - rw.value
                            FROM    (VALUES {values[..^1]}) as rw(delegate, value)
                            WHERE   sb.""Level"" = {Block.Level}
                            AND     sb.""AccountId"" = rw.delegate;");
                    }
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
        public static async Task<SnapshotBalanceCommit> Apply(ProtocolHandler proto, IBlock rawBlock, Block block)
        {
            var commit = new SnapshotBalanceCommit(proto) { RawBlock = rawBlock as RawBlock, Block = block };
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
