using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegatorCycleCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block, Cycle? futureCycle)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await CreateFromSnapshots(futureCycle!);

            #region weird delegators
            if (block.Cycle > 0)
            {
                //one-way change...
                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "DelegatorCycles" as dc
                    USING "Accounts" as acc
                    WHERE acc."Id" = dc."BakerId"
                    AND dc."Cycle" = {0}
                    AND acc."Type" != {1}
                    """, block.Cycle - 1, (int)AccountType.Delegate);
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                var futureCycle = block.Cycle + Context.Protocol.ConsensusRightsDelay;

                await Db.Database.ExecuteSqlRawAsync("""
                    DELETE FROM "DelegatorCycles"
                    WHERE "Cycle" = {0}
                    """, futureCycle);
            }
        }

        protected virtual Task CreateFromSnapshots(Cycle futureCycle)
        {
            return Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "DelegatorCycles" (
                    "Cycle",
                    "DelegatorId",
                    "BakerId",
                    "DelegatedBalance",
                    "StakedBalance"
                )
                SELECT
                    {0},
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "OwnStakedBalance"
                FROM "SnapshotBalances"
                WHERE "Level" = {1}
                AND "AccountId" != "BakerId"
                """, futureCycle.Index, futureCycle.SnapshotLevel);
        }
    }
}
