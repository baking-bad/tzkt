using Netezos.Encoding;
using Netezos.Rpc;

namespace Tzkt.Api.Services
{
    public sealed class RpcHelpers : IDisposable
    {
        readonly TezosRpc Rpc;
        string ChainId;

        public RpcHelpers(IConfiguration config)
        {
            var _config = config.GetRpcHelpersConfig();
            Rpc = _config.Enabled ? new TezosRpc(_config.Endpoint, _config.Timeout) : null;
        }

        public async Task<string> GetChainId()
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");

            return ChainId ??= await Rpc.GetAsync<string>("chains/main/chain_id");
        }

        public async Task<string> Inject(string content, bool async)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");

            return await Rpc.Inject.Operation.PostAsync<string>(content, async);
        }
        
        public async Task<IMicheline> RunScriptView(string contract, string view, IMicheline input)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");
            
            return (await Rpc.Blocks.Head.Helpers.Scripts.RunScriptView.PostAsync(contract, view, input, await GetChainId())).data;
        }

        public void Dispose() => Rpc?.Dispose();
    }
}
