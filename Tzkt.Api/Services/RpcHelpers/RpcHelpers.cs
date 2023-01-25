using System.Text.Json;
using Netezos.Encoding;
using Netezos.Rpc;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Services
{
    public sealed class RpcHelpers : IDisposable
    {
        readonly StateCache State;
        readonly TezosRpc Rpc;
        readonly AccountRepository Accounts;


        public RpcHelpers(StateCache state, IConfiguration config, AccountRepository accounts)
        {
            State = state;
            Accounts = accounts;
            var _config = config.GetRpcHelpersConfig();
            Rpc = _config.Enabled ? new TezosRpc(_config.Endpoint, _config.Timeout) : null;
        }

        public async Task<string> Inject(string content, bool async)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");

            return await Rpc.Inject.Operation.PostAsync<string>(content, async);
        }
        
        public async Task<string> RunScriptView(string contract, string view, string input = null)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");
            
            using var doc = JsonDocument.Parse(input);
            var builtInput = await Accounts.BuildViewInput(contract, view, doc.RootElement);
            if (builtInput == null) return null;
            
            var res = await Rpc.Blocks.Head.Helpers.Scripts.RunScriptView.PostAsync(contract, view, builtInput, State.Current.ChainId);
            return await Accounts.BuildViewOutput(contract, view, (IMicheline)res.data);
        }
        
        public async Task<string> RunScriptView(string contract, string view, object input = null)
        {
            if (Rpc == null)
                throw new InvalidOperationException("RpcHelpers disabled");
            
            var builtInput = await Accounts.BuildViewInput(contract, view, input);
            if (builtInput == null) return null;

            var res = await Rpc.Blocks.Head.Helpers.Scripts.RunScriptView.PostAsync(contract, view, builtInput, State.Current.ChainId);
            return await Accounts.BuildViewOutput(contract, view, (IMicheline)res.data);
        }
        public void Dispose() => Rpc?.Dispose();
    }
}
