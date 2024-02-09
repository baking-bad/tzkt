using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto12
{
    class SnapshotBalanceCommit : Proto9.SnapshotBalanceCommit
    {
        public SnapshotBalanceCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task SubtractCycleRewards(JsonElement rawBlock, Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            await Db.Database.ExecuteSqlRawAsync($"""
                UPDATE "SnapshotBalances" as sb
                SET "OwnDelegatedBalance" = "OwnDelegatedBalance" - bc."EndorsementRewardsDelegated"	                        
                FROM (
                    SELECT "BakerId", "EndorsementRewardsDelegated"
                    FROM "BakerCycles"
                    WHERE "Cycle" = {block.Cycle}
                    AND "EndorsementRewardsDelegated" != 0
                ) as bc
                WHERE sb."Level" = {block.Level}
                AND sb."AccountId" = bc."BakerId"
                AND sb."BakerId" = bc."BakerId"
                """);
        }
    }
}
