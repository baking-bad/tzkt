using Microsoft.Extensions.Configuration;

namespace Tzkt.Sync.Services
{
    public class ObserverConfig
    {
    }

    public static class ObserverConfigExt
    {
        public static ObserverConfig GetObserverConfig(this IConfiguration config)
        {
            return config.GetSection("Observer")?.Get<ObserverConfig>() ?? new ObserverConfig();
        }
    }
}
