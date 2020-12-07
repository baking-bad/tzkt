using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto3
{
    class DelegatorCycleCommit : Proto1.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply(Block block, Cycle futureCycle)
        {
            if (block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT  INTO ""DelegatorCycles"" (""Cycle"", ""DelegatorId"", ""BakerId"", ""Balance"")
                    SELECT  {futureCycle.Index}, ""AccountId"", ""DelegateId"", ""Balance""
                    FROM    ""SnapshotBalances""
                    WHERE   ""Level"" = {futureCycle.SnapshotLevel}
                    AND     ""DelegateId"" IS NOT NULL");
            }
        }
    }
}
