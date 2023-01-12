using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Websocket
{
    public class WebsocketConfig
    {
        public bool Enabled { get; set; } = true;
        public int MaxConnections { get; set; } = 1000;
        public int MaxOperationSubscriptions { get; set; } = 50;
        public int MaxBigMapSubscriptions { get; set; } = 50;
        public int MaxEventSubscriptions { get; set; } = 50;
        public int MaxAccountsSubscriptions { get; set; } = 50;
        public int MaxTokenBalancesSubscriptions { get; set; } = 50;
        public int MaxTokenTransfersSubscriptions { get; set; } = 50;
    }

    public static class CacheConfigExt
    {
        public static WebsocketConfig GetWebsocketConfig(this IConfiguration config)
        {
            return config.GetSection("Websocket")?.Get<WebsocketConfig>() ?? new WebsocketConfig();
        }
    }
}