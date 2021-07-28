using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tzkt.Api.Websocket.Processors;

namespace Tzkt.Api.Websocket.Hubs
{
    public class DefaultHub : BaseHub
    {
        readonly HeadProcessor<DefaultHub> Head;
        readonly BlocksProcessor<DefaultHub> Blocks;
        readonly OperationsProcessor<DefaultHub> Operations;
        readonly BigMapsProcessor<DefaultHub> BigMaps;
        readonly AccountProcessor<DefaultHub> Account;

        public DefaultHub(
            HeadProcessor<DefaultHub> head,
            BlocksProcessor<DefaultHub> blocks,
            OperationsProcessor<DefaultHub> operations,
            BigMapsProcessor<DefaultHub> bigMaps,
            AccountProcessor<DefaultHub> account,
            ILogger<DefaultHub> logger,
            IConfiguration config) : base(logger, config)
        {
            Head = head;
            Blocks = blocks;
            Operations = operations;
            BigMaps = bigMaps;
            Account = account;
        }

        public Task SubscribeToHead()
        {
            return Head.Subscribe(Clients.Caller, Context.ConnectionId);
        }

        public Task SubscribeToBlocks()
        {
            return Blocks.Subscribe(Clients.Caller, Context.ConnectionId);
        }

        public Task SubscribeToOperations(OperationsParameter parameters)
        {
            parameters.EnsureValid();
            return Operations.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task SubscribeToBigMaps(BigMapsParameter parameters)
        {
            parameters.EnsureValid();
            return BigMaps.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }
        
        public Task SubscribeToAccount(AccountsParameter parameters)
        {
            parameters.EnsureValid();
            return Account.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Operations.Unsubscribe(Context.ConnectionId);
            BigMaps.Unsubscribe(Context.ConnectionId);
            Account.Unsubscribe(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}