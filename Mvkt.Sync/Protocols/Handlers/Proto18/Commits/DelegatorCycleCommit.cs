using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto18
{
    class DelegatorCycleCommit : Proto3.DelegatorCycleCommit
    {
        public DelegatorCycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override Task CreateFromSnapshots(Cycle futureCycle)
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
