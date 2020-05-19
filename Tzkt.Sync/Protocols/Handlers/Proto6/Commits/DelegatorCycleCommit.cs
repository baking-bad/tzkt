using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto6
{
    class DelegatorCycleCommit : ProtocolCommit
    {
        public Block Block { get; private set; }
        public Cycle FutureCycle { get; private set; }

        DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        public override async Task Apply()
        {
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                await Db.Database.ExecuteSqlRawAsync($@"
                    INSERT  INTO ""DelegatorCycles"" (""Cycle"", ""DelegatorId"", ""BakerId"", ""Balance"")
                    SELECT  {FutureCycle.Index}, ""AccountId"", ""DelegateId"", ""Balance""
                    FROM    ""SnapshotBalances""
                    WHERE   ""Level"" = {FutureCycle.SnapshotLevel}
                    AND     ""DelegateId"" IS NOT NULL");
            }
        }

        public override async Task Revert()
        {
            if (Block.Events.HasFlag(BlockEvents.CycleBegin))
            {
                Block.Protocol ??= await Cache.Protocols.GetAsync(Block.ProtoCode);
                var futureCycle = (Block.Level - 1) / Block.Protocol.BlocksPerCycle + Block.Protocol.PreservedCycles;

                await Db.Database.ExecuteSqlRawAsync($@"
                    DELETE  FROM ""DelegatorCycles""
                    WHERE   ""Cycle"" = {futureCycle}");
            }
        }

        #region static
        public static async Task<DelegatorCycleCommit> Apply(ProtocolHandler proto, Block block, Cycle futureCycle)
        {
            var commit = new DelegatorCycleCommit(proto) { Block = block, FutureCycle = futureCycle };
            await commit.Apply();
            return commit;
        }

        public static async Task<DelegatorCycleCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new DelegatorCycleCommit(proto) { Block = block };
            await commit.Revert();
            return commit;
        }
        #endregion
    }
}
