using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Netezos.Rpc;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public sealed class NodeRpc : IDisposable
    {
        readonly StateCache State;
        readonly ILogger Logger;

        readonly TezosRpc Rpc;

        public NodeRpc(IConfiguration config, StateCache state, ILogger<NodeRpc> logger)
        {
            State = state;
            Logger = logger;
            var nodeConf = config.GetTezRpcConfig();
            Rpc = new TezosRpc(nodeConf.Endpoint, nodeConf.Timeout);
        }

        public async Task<string> Send(string content, bool async)
        {
            return await Rpc.Inject.Operation.PostAsync<string>(content, async);
        }
        
        public void Dispose() => Rpc.Dispose();

        public async Task<string> GetChainIdAsync()
        {
            return (await Rpc.Blocks.Head.Header.GetAsync()).chain_id;
        }
    }
}
