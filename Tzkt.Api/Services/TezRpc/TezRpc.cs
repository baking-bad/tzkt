using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Netezos.Encoding;
using Netezos.Forging;
using Netezos.Rpc;
using Tzkt.Api.Models.Send;
using Tzkt.Api.Services.Cache;
using Tzkt.Data.Models.Base;

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
            //TODO Consider the disabling function
            //TODO Should initialize at start and fail the starting process if the chain ID doesn't match
            logger.LogDebug("Initializing TezRpc...");
            State = state;
            Logger = logger;
            var nodeConf = config.GetTezRpcConfig();
            Rpc = new TezosRpc($"{nodeConf.Endpoint.TrimEnd('/')}/", nodeConf.Timeout);
            var chainId = Rpc.Blocks.Head.Header.GetAsync().Result.chain_id.ToString();
            if (chainId != State.Current.ChainId)
            {
                logger.LogDebug($"The API chain ID {chainId.ToString()} doesn't match the indexer chain ID {State.Current.ChainId}." +
                                $" Please, check that the indexer and API use the same network");

                throw new ArgumentException($"The API chain ID {chainId.ToString()} doesn't match the indexer chain ID {State.Current.ChainId}." +
                                            $" Please, check that the indexer and API use the same network");
            }
        }

        public async Task<string> Send(string content)
        {
            //TODO Handle too low fees
            //TODO All required checks for the content data
            var forge = new LocalForge();
            var delimiter = content.Length - 128;
            var bytes = Hex.Parse(content[..delimiter]);
            var body = await forge.UnforgeOperationAsync(bytes);
            var protocol = (string)(await Rpc.Blocks.Head.Header.ProtocolData.GetAsync()).protocol;
            var signature = Base58.Convert(Hex.Parse(content[delimiter..]));
            var contents = body.Item2.ToList();
            return await Rpc.Inject.Operation.PostAsync<string>(content);
        }
        
        public void Dispose() => Rpc.Dispose();
    }
}
