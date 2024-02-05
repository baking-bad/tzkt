using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
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
                    snapshot."AccountId",
                    snapshot."BakerId",
                    snapshot."OwnDelegatedBalance",
                    (baker."ExternalStakedBalance"::numeric * snapshot."StakedPseudotokens" / GREATEST(baker."IssuedPseudotokens", 1))::bigint
                FROM "SnapshotBalances" as snapshot
                INNER JOIN "SnapshotBalances" as baker
                        ON baker."AccountId" = snapshot."BakerId" AND baker."BakerId" = snapshot."BakerId" and baker."Level" = snapshot."Level"
                WHERE snapshot."Level" = {futureCycle.SnapshotLevel}
                AND snapshot."AccountId" != snapshot."BakerId"
                """);
        }
    }
}
