namespace Tzkt.Sync.Services.Observer.Notifiers
{
    class LaggedStreamingHeadNotifier(int _lag, TezosNode _node, ILogger _logger) : HeadNotifier(_logger)
    {
        protected override string Parameters => $"method: streaming, lag: {_lag}";

        readonly Dictionary<int, Header> _cache = new(_lag + 4);

        protected override async Task OnTick(CancellationToken cancellationToken)
        {
            await foreach (var head in _node.MonitorAsync<Header>("monitor/heads/main", cancellationToken))
            {
                if (!_cache.TryGetValue(head.Level - 1, out var predecessor) || predecessor.Hash != head.Predecessor)
                    _cache.Clear();

                if (!_cache.TryGetValue(head.Level - _lag, out var laggedHead))
                {
                    laggedHead = await _node.GetAsync<Header>($"chains/main/blocks/{head.Level - _lag}/header");
                    _cache[laggedHead.Level] = laggedHead;
                }

                _cache.Remove(laggedHead.Level - 3);
                _cache[head.Level] = head;

                Notify(laggedHead);
            }
        }
    }
}
