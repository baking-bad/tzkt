using Microsoft.Extensions.Configuration;

namespace Mvkt.Sync.Services
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
