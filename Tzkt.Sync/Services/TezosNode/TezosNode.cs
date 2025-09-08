using System.Text.Json;

namespace Tzkt.Sync.Services
{
    public sealed class TezosNode : IDisposable
    {
        readonly TzktClient _client;
        readonly ILogger _logger;

        public TezosNode(IConfiguration config, ILogger<TezosNode> logger)
        {
            var nodeConfig = config.GetTezosNodeConfig();
            _client = new TzktClient(nodeConfig.Endpoint, nodeConfig.Timeout);
            _logger = logger;
        }

        public async Task<JsonElement> GetAsync(string url)
        {
            try
            {
                return await _client.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({url}) failed", url);
                throw;
            }
        }

        public async Task<JsonElement> GetAsync(string url, TimeSpan timeout)
        {
            try
            {
                return await _client.GetAsync(url, timeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({url}) failed", url);
                throw;
            }
        }

        public async Task<T> GetAsync<T>(string url)
        {
            try
            {
                return await _client.GetAsync<T>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({url}) failed", url);
                throw;
            }
        }

        public Task<JsonElement> PostAsync(string url, string content)
        {
            try
            {
                return _client.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({url}) failed", url);
                throw;
            }
        }

        public IAsyncEnumerable<T> MonitorAsync<T>(string url, CancellationToken cancellationToken)
        {
            try
            {
                return _client.GetStreamAsync<T>(url, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({url}) failed", url);
                throw;
            }
        }

        public void Dispose() => _client.Dispose();
    }

    public static class TezosNodeExt
    {
        public static void AddTezosNode(this IServiceCollection services)
        {
            services.AddSingleton<TezosNode>();
        }
    }
}
