using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class DelegatorCycleCommit : Proto3.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task CreateFromSnapshots(Cycle futureCycle)
        {
            return Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "DelegatorCycles" (
                    "Cycle",
                    "DelegatorId",
                    "BakerId",
                    "DelegatedBalance",
                    "StakedPseudotokens"
                )
                SELECT
                    {0},
                    "AccountId",
                    "BakerId",
                    "OwnDelegatedBalance",
                    "Pseudotokens"
                FROM "SnapshotBalances"
                WHERE "Level" = {1}
                AND "BakerId" != "AccountId"
                """, futureCycle.Index, futureCycle.SnapshotLevel);
        }
    }
}
