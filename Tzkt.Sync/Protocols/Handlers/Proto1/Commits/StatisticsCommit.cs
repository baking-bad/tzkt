using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    class StatisticsCommit : ProtocolCommit
    {
        public StatisticsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(JsonElement rawBlock)
        {
            var prev = Cache.Statistics.Current;
            var statistics = new Statistics
            {
                Level = prev.Level + 1,
                TotalActivated = prev.TotalActivated,
                TotalBootstrapped = prev.TotalBootstrapped,
                TotalBurned = prev.TotalBurned,
                TotalBanished = prev.TotalBanished,
                TotalCommitments = prev.TotalCommitments,
                TotalCreated = prev.TotalCreated,
                TotalFrozen = prev.TotalFrozen,
                TotalRollupBonds = prev.TotalRollupBonds,
                TotalSmartRollupBonds = prev.TotalSmartRollupBonds
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
            await Db.Database.ExecuteSqlRawAsync($"""DELETE FROM "Statistics" WHERE "Level" = {block.Level}""");
            await Cache.Statistics.ResetAsync();
        }
    }
}
