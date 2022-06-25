using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Netezos.Rpc;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public sealed class TezRpc : IDisposable
    {
        readonly StateCache State;
        readonly ILogger Logger;

        //TODO Consider changing the class name to `SenderRpc` or `ApiRpc`
        readonly TezosRpc Rpc;

        public TezRpc(IConfiguration config, StateCache state, ILogger<TezRpc> logger)
        {
            logger.LogDebug("Initializing TezRpc...");
            State = state;
            Logger = logger;
            var nodeConf = config.GetTezRpcConfig();
            Rpc = new TezosRpc($"{nodeConf.Endpoint}", nodeConf.Timeout);
        }

        public async Task<string> Send(string content, bool async)
        {
            return await Rpc.Inject.Operation.PostAsync<string>(content, async);
        }
        
        public void Dispose() => Rpc.Dispose();
    }
}
