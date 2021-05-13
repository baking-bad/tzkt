using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Services.Stats
{
    public class StatsConfig
    {
        public bool Enabled { get; set; } = false;
        public int UpdatePeriod { get; set; } = 100;
    }

    public static class StatsConfigExt
    {
        public static StatsConfig GetStatsConfig(this IConfiguration config)
        {
            return config.GetSection("Stats")?.Get<StatsConfig>() ?? new StatsConfig();
        }
    }
}