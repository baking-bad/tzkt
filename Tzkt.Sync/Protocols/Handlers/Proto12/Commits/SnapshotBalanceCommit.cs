using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
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
                    INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"", ""DelegatorsCount"", ""DelegatedBalance"", ""StakingBalance"")
                    SELECT {block.Level}, ""Balance"", ""Id"", ""DelegateId"", ""DelegatorsCount"", ""DelegatedBalance"", ""StakingBalance""
                    FROM ""Accounts""
                    WHERE ""Staked"" = true");
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
                            INSERT INTO ""SnapshotBalances"" (""Level"", ""Balance"", ""AccountId"", ""DelegateId"", ""DelegatorsCount"", ""DelegatedBalance"", ""StakingBalance"") VALUES ";

                        foreach (var baker in deactivated)
                        {
                            sql += $@"
                                ({block.Level}, {baker.Balance}, {baker.Id}, NULL, {baker.DelegatorsCount}, {baker.DelegatedBalance}, {baker.StakingBalance}),";

                            foreach (var delegator in baker.DelegatedAccounts)
                                sql += $@"
                                    ({block.Level}, {delegator.Balance}, {delegator.Id}, {delegator.DelegateId}, NULL, NULL, NULL),";
                        }

                        await Db.Database.ExecuteSqlRawAsync(sql[..^1]);
                    }
                }
                #endregion

                #region revert endorsing rewards
                if (block.Events.HasFlag(BlockEvents.CycleEnd))
                {
                    await Db.Database.ExecuteSqlRawAsync($@"
                        UPDATE ""SnapshotBalances"" as sb
                        SET ""Balance"" = ""Balance"" - bc.""EndorsementRewards"",
                            ""StakingBalance"" = ""StakingBalance"" - bc.""EndorsementRewards""	                        
                        FROM (
	                        SELECT ""BakerId"", ""EndorsementRewards""
	                        FROM ""BakerCycles""
	                        WHERE ""Cycle"" = {block.Cycle}
                            AND ""EndorsementRewards"" != 0
                        ) as bc
                        WHERE sb.""Level"" = {block.Level}
                        AND sb.""DelegateId"" IS NULL
                        AND sb.""AccountId"" = bc.""BakerId""");
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
    }
}
