using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class DelegatorCycleCommit : ProtocolCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block, Cycle futureCycle)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT  INTO ""DelegatorCycles"" (""Cycle"", ""DelegatorId"", ""BakerId"", ""Balance"")
                    SELECT  {futureCycle.Index}, ""AccountId"", ""DelegateId"", ""Balance""
                    FROM    ""SnapshotBalances""
                    WHERE   ""Level"" = {futureCycle.SnapshotLevel}
                    AND     ""DelegateId"" IS NOT NULL");

                #region weird delegators
                if (block.Cycle > 0)
                {
                    //one-way change...
                    await Db.Database.ExecuteSqlRawAsync($@"
                        DELETE FROM ""DelegatorCycles"" as dc
                        USING ""Accounts"" as acc
                        WHERE acc.""Id"" = dc.""BakerId""
                        AND dc.""Cycle"" = {block.Cycle - 1}
                        AND acc.""Type"" != {(int)AccountType.Delegate}");
                }
                #endregion
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                block.Protocol ??= await Cache.Protocols.GetAsync(block.ProtoCode);
                var futureCycle = block.Cycle + block.Protocol.PreservedCycles;

                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""DelegatorCycles""
                    WHERE   ""Cycle"" = {futureCycle}");
            }
        }
    }
}
