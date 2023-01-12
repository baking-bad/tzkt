using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dynamic.Json;
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
                string chainId = await DJson.GetAsync($"{config.Endpoint.TrimEnd('/')}/chains/main/chain_id");

                if (chainId == services.GetRequiredService<StateCache>().Current.ChainId)
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
