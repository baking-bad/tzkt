using App.Metrics;
using Tzkt.Api.Websocket.Processors;

namespace Tzkt.Api.Websocket.Hubs
{
    public class DefaultHub : BaseHub
    {
        readonly HeadProcessor<DefaultHub> Head;
        readonly CyclesProcessor<DefaultHub> Cycles;
        readonly BlocksProcessor<DefaultHub> Blocks;
        readonly OperationsProcessor<DefaultHub> Operations;
        readonly BigMapsProcessor<DefaultHub> BigMaps;
        readonly EventsProcessor<DefaultHub> Events;
        readonly TokenBalancesProcessor<DefaultHub> TokenBalances;
        readonly TokenTransfersProcessor<DefaultHub> TokenTransfers;
        readonly TicketBalancesProcessor<DefaultHub> TicketBalances;
        readonly TicketTransfersProcessor<DefaultHub> TicketTransfers;
        readonly AccountsProcessor<DefaultHub> Accounts;

        public DefaultHub(
            HeadProcessor<DefaultHub> head,
            CyclesProcessor<DefaultHub> cycles,
            BlocksProcessor<DefaultHub> blocks,
            OperationsProcessor<DefaultHub> operations,
            BigMapsProcessor<DefaultHub> bigMaps,
            EventsProcessor<DefaultHub> events,
            TokenBalancesProcessor<DefaultHub> tokenBalances,
            TokenTransfersProcessor<DefaultHub> tokenTransfers,
            TicketBalancesProcessor<DefaultHub> ticketBalances,
            TicketTransfersProcessor<DefaultHub> ticketTransfers,
            AccountsProcessor<DefaultHub> accounts,
            ILogger<DefaultHub> logger,
            IMetrics metrics,
            IConfiguration config) : base(logger, config, metrics)
        {
            Head = head;
            Cycles = cycles;
            Blocks = blocks;
            Operations = operations;
            BigMaps = bigMaps;
            Events = events;
            TokenBalances = tokenBalances;
            TokenTransfers = tokenTransfers;
            TicketBalances = ticketBalances;
            TicketTransfers = ticketTransfers;
            Accounts = accounts;
        }

        public Task<int> SubscribeToHead()
        {
            return Head.Subscribe(Clients.Caller, Context.ConnectionId);
        }

        public Task<int> SubscribeToCycles(CyclesParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return Cycles.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToBlocks()
        {
            return Blocks.Subscribe(Clients.Caller, Context.ConnectionId);
        }

        public Task<int> SubscribeToOperations(OperationsParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return Operations.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToBigMaps(BigMapsParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return BigMaps.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToEvents(EventsParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return Events.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToTokenBalances(TokenTransfersParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return TokenBalances.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToTokenTransfers(TokenTransfersParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return TokenTransfers.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToTicketBalances(TicketTransfersParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return TicketBalances.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToTicketTransfers(TicketTransfersParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return TicketTransfers.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public Task<int> SubscribeToAccounts(AccountsParameter? parameters)
        {
            parameters ??= new();
            parameters.EnsureValid();
            return Accounts.Subscribe(Clients.Caller, Context.ConnectionId, parameters);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Cycles.Unsubscribe(Context.ConnectionId);
            Operations.Unsubscribe(Context.ConnectionId);
            BigMaps.Unsubscribe(Context.ConnectionId);
            Events.Unsubscribe(Context.ConnectionId);
            TokenBalances.Unsubscribe(Context.ConnectionId);
            TokenTransfers.Unsubscribe(Context.ConnectionId);
            TicketBalances.Unsubscribe(Context.ConnectionId);
            TicketTransfers.Unsubscribe(Context.ConnectionId);
            Accounts.Unsubscribe(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}