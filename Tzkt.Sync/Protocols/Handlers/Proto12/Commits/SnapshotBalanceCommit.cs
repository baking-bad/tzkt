using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto12
{
    class SnapshotBalanceCommit : Proto9.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task SubtractCycleRewards(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync("""
                UPDATE "SnapshotBalances" as sb
                SET "OwnDelegatedBalance" = "OwnDelegatedBalance" - bc."AttestationRewardsDelegated"	                        
                FROM (
                    SELECT "BakerId", "AttestationRewardsDelegated"
                    FROM "BakerCycles"
                    WHERE "Cycle" = {0}
                    AND "AttestationRewardsDelegated" != 0
                ) as bc
                WHERE sb."Level" = {1}
                AND sb."BakerId" = bc."BakerId"
                AND sb."AccountId" = bc."BakerId"
                """, block.Cycle, block.Level);
        }
    }
}
