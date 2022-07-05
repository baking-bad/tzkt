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
        readonly bool Enabled;
        readonly TezosRpc Rpc;

        public NodeRpc(IConfiguration config)
        {
            var nodeConf = config.GetNodeRpcConfig();
            Enabled = nodeConf.Enabled;
            Rpc = Enabled ? new TezosRpc(nodeConf.Endpoint, nodeConf.Timeout) : null;
        }

        public async Task<string> Send(string content, bool async)
        {
            return Enabled ? await Rpc.Inject.Operation.PostAsync<string>(content, async) : null;
        }
        
        public void Dispose() => Rpc.Dispose();

        public async Task<string> GetChainIdAsync()
        {
            return Enabled ? (await Rpc.Blocks.Head.Header.GetAsync()).chain_id : null;
        }
    }
}
