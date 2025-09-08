namespace Tzkt.Sync.Services.Observer.Notifiers
{
    class PollingHeadNotifier(int _lag, int _period, TezosNode _node, ILogger _logger) : HeadNotifier(_logger)
    {
        protected override string Parameters => $"method: polling, period: {_period}ms, lag: {_lag}";

        protected override async Task OnTick(CancellationToken cancellationToken)
        {
            var head = await _node.GetAsync<Header>(_lag != 0
                ? $"chains/main/blocks/head~{_lag}/header"
                : "chains/main/blocks/head/header");

            Notify(head);

            await Task.Delay(_period, cancellationToken);
        }
    }
}
