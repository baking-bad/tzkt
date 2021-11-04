using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        public string BaseUrl { get; }
        readonly TzktClient Rpc;

        Header Header;
        Constants Constants;
        DateTime NextBlock;

        public TezosNode(IConfiguration config)
        {
            var nodeConf = config.GetTezosNodeConfig();
            BaseUrl = $"{nodeConf.Endpoint.TrimEnd('/')}/";
            Rpc = new TzktClient(BaseUrl, nodeConf.Timeout);
        }

        public async Task<JsonElement> GetAsync(string url)
        {
            using var stream = await Rpc.GetStreamAsync(url);
            using var doc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 100_000 });
            return doc.RootElement.Clone();
        }

        public async Task<Header> GetHeaderAsync()
        {
            if (DateTime.UtcNow >= NextBlock)
            {
                var header = await Rpc.GetObjectAsync<Header>("chains/main/blocks/head/header");

                if (header.Protocol != Header?.Protocol)
                    Constants = await Rpc.GetObjectAsync<Constants>("chains/main/blocks/head/context/constants");

                NextBlock = header.Level != Header?.Level
                    ? header.Timestamp.AddSeconds(Constants.MinBlockDelay ?? Constants.BlockIntervals[0])
                    : DateTime.UtcNow.AddSeconds(1);

                Header = header;
            }

            return Header;
        }

        public Task<Header> GetHeaderAsync(int level)
        {
            return Rpc.GetObjectAsync<Header>($"chains/main/blocks/{level}/header");
        }

        public async Task<bool> HasUpdatesAsync(int level)
        {
            var header = await GetHeaderAsync();
            return header.Level != level;
        }

        public void Dispose() => Rpc.Dispose();
    }

    public static class TezosNodeExt
    {
        public static void AddTezosNode(this IServiceCollection services)
        {
            services.AddSingleton<TezosNode>();
        }
    }
}
