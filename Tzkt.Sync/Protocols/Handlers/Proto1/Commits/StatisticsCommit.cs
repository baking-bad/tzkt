using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class StatisticsCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(JsonElement rawBlock)
        {
            var prev = Cache.Statistics.Current;
            var statistics = new Statistics
            {
                Id = 0,
                Level = prev.Level + 1,
                TotalActivated = prev.TotalActivated,
                TotalBootstrapped = prev.TotalBootstrapped,
                TotalBurned = prev.TotalBurned,
                TotalBanished = prev.TotalBanished,
                TotalLost = prev.TotalLost,
                TotalCommitments = prev.TotalCommitments,
                TotalCreated = prev.TotalCreated,
                TotalFrozen = prev.TotalFrozen,
                TotalRollupBonds = prev.TotalRollupBonds,
                TotalSmartRollupBonds = prev.TotalSmartRollupBonds,
                TotalOwnStaked = prev.TotalOwnStaked,
                TotalExternalStaked = prev.TotalExternalStaked,
                TotalOwnDelegated = prev.TotalOwnDelegated,
                TotalExternalDelegated = prev.TotalExternalDelegated,
                TotalBakingPower = prev.TotalBakingPower,
                TotalVotingPower = prev.TotalVotingPower,
                TotalBakers = prev.TotalBakers,
                TotalStakers = prev.TotalStakers,
                TotalDelegators = prev.TotalDelegators
            };

            var protocol = await Cache.Protocols.GetAsync(rawBlock.RequiredString("protocol"));
            if (protocol.IsCycleEnd(statistics.Level))
                statistics.Cycle = protocol.GetCycle(statistics.Level);

            var timestamp = rawBlock.Required("header").RequiredDateTime("timestamp");
            var prevTimestamp = (await Cache.Blocks.GetAsync(prev.Level)).Timestamp;
            if (timestamp.Ticks / (10_000_000L * 3600 * 24) != prevTimestamp.Ticks / (10_000_000L * 3600 * 24))
            {
                Db.TryAttach(prev);
                prev.Date = prevTimestamp.Date;
            }

            Db.Statistics.Add(statistics);
            Cache.Statistics.SetCurrent(statistics);
        }

        public virtual async Task Revert(Block block)
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "Statistics"
                WHERE "Level" = {0}
                """, block.Level);
            await Cache.Statistics.ResetAsync();
        }
    }
}
