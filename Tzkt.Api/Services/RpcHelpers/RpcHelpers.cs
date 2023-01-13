using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netezos.Rpc;

namespace Tzkt.Api.Services
{
    public sealed class RpcHelpers : IDisposable
    {
        readonly TezosRpc Rpc;

        public RpcHelpers(IConfiguration config)
        {
            var _config = config.GetRpcHelpersConfig();
            Rpc = _config.Enabled ? new TezosRpc(_config.Endpoint, _config.Timeout) : null;
        }

        public async Task<string> Inject(string content, bool async)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");

            return await Rpc.Inject.Operation.PostAsync<string>(content, async);
        }
        
        public void Dispose() => Rpc?.Dispose();
    }
}
