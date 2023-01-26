using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public class RpcHelpersConfig
    {
        public bool Enabled { get; set; } = false;
        public string Endpoint { get; set; } = "https://rpc.tzkt.io/mainnet";
        public int Timeout { get; set; } = 60;
    }

    public static class TezRpcConfigExt
    {
        public static RpcHelpersConfig GetRpcHelpersConfig(this IConfiguration config)
        {
            return config.GetSection("RpcHelpers")?.Get<RpcHelpersConfig>() ?? new();
        }

        public static async Task ValidateRpcHelpersConfig(this IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>().GetRpcHelpersConfig();
            
            if (!config.Enabled)
                return;
            
            if (!Uri.IsWellFormedUriString(config.Endpoint, UriKind.Absolute))
                throw new ConfigurationException("RpcHelpers.Endpoint is invalid");
            
            try
            {
                var helpers = services.GetRequiredService<RpcHelpers>();
                var state = services.GetRequiredService<StateCache>();

                if (await helpers.GetChainId() != state.Current.ChainId)
                    throw new ConfigurationException("RpcHelpers.Endpoint refers to the node with different chain_id");
            }
            catch (ConfigurationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Failed to validate RpcHelpers config: {ex.Message}");
            }
        }
    }
}
