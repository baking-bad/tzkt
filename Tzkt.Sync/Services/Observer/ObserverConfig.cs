using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class ObserverConfig
    {
        public int Lag { get; set; } = 0;  // in blocks
    }

    public static class ObserverConfigExt
    {
        public static ObserverConfig GetObserverConfig(this IConfiguration config)
        {
            return config.GetSection("Observer")?.Get<ObserverConfig>() ?? new ObserverConfig();
        }
    }
}
