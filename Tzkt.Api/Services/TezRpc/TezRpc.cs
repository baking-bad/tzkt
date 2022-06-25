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
            InitializeAsync();
        }

        async Task InitializeAsync()
        {
            var chainId = (await Rpc.Blocks.Head.Header.GetAsync()).chain_id.ToString();
            if (chainId != State.Current.ChainId)
            {
                throw new ArgumentException($"The API chain ID {chainId.ToString()} doesn't match the indexer chain ID {State.Current.ChainId}." +
                                            $" Please, check that the indexer and API use the same network");
            }
        }

        public async Task<string> Send(string content)
        {
            return await Rpc.Inject.Operation.PostAsync<string>(content);
        }
        
        public void Dispose() => Rpc.Dispose();
    }
}
