using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegatorCycleCommit : ProtocolCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, Cycle futureCycle)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            await CreateFromSnapshots(futureCycle);

            #region weird delegators
            if (block.Cycle > 0)
            {
                //one-way change...
                await Db.Database.ExecuteSqlRawAsync($"""
                    DELETE FROM "DelegatorCycles" as dc
                    USING "Accounts" as acc
                    WHERE acc."Id" = dc."BakerId"
                    AND dc."Cycle" = {block.Cycle - 1}
                    AND acc."Type" != {(int)AccountType.Delegate}
                    """);
            }
            #endregion
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
                var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

                await Db.Database.ExecuteSqlRawAsync($"""
                    DELETE FROM "DelegatorCycles"
                    WHERE "Cycle" = {futureCycle}
                    """);
            }
        }

        protected virtual Task CreateFromSnapshots(Cycle futureCycle)
        {
            return Db.Database.ExecuteSqlRawAsync($"""
                INSERT INTO "DelegatorCycles" (
                    "Cycle",
                    "DelegatorId",
                    "BakerId",
                    "DelegatedBalance",
                    "StakedBalance"
                )
                SELECT
                    {futureCycle.Index},
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "OwnStakedBalance"
                FROM "SnapshotBalances"
                WHERE "Level" = {futureCycle.SnapshotLevel}
                AND "AccountId" != "BakerId"
                """);
        }
    }
}
