using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services
{
    public sealed class EvmNode : IDisposable
    {
        readonly TzktClient _client;
        readonly ILogger _logger;

        public EvmNode(IConfiguration config, ILogger<EvmNode> logger)
        {
            var nodeConfig = config.GetEvmNodeConfig();
            _client = new TzktClient(nodeConfig.Endpoint, nodeConfig.Timeout);
            _logger = logger;
        }

        public async Task<T> PostAsync<T>(string method, params object[] args)
        {
            try
            {
                var request = new JsonRpcRequest(method, args);
                var response = await _client.PostAsync<JsonRpcResponse<T>>(string.Empty, JsonSerializer.Serialize(request));
                
                if (response.RequestId != request.Id)
                    throw new Exception("Invalid RPC response id");

                if (response.Error != null)
                    throw new Exception(response.Error.Message);

                return response.Result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC request ({method}) failed", method);
                throw;
            }
        }

        public void Dispose() => _client.Dispose();

        class JsonRpcRequest(string method, params object[] args)
        {
            [JsonPropertyName("jsonrpc")]
            public string Version { get; private set; } = "2.0";

            [JsonPropertyName("id")]
            public int Id { get; private set; } = 0;

            [JsonPropertyName("method")]
            public string Method { get; private set; } = method;

            [JsonPropertyName("params")]
            public object[] Params { get; private set; } = args;
        }

        class JsonRpcResponse<T>
        {
            [JsonPropertyName("jsonrpc")]
            public required string Version { get; set; }

            [JsonPropertyName("id")]
            public required int RequestId { get; set; }

            [JsonPropertyName("result")]
            public T? Result { get; set; }

            [JsonPropertyName("error")]
            public JsonRpcError? Error { get; set; }
        }

        class JsonRpcError
        {
            [JsonPropertyName("code")]
            public required int Code { get; set; }

            [JsonPropertyName("message")]
            public required string Message { get; set; }
        }
    }

    public static class EvmNodeExt
    {
        public static void AddEvmNode(this IServiceCollection services)
        {
            services.AddSingleton<EvmNode>();
        }
    }
}
