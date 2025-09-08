using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services.Observer.Notifiers
{
    class DebouncedPollingHeadNotifier(int _lag, int _period, TezosNode _node, ILogger _logger) : HeadNotifier(_logger)
    {
        protected override string Parameters => $"method: debounced polling, period: {_period}ms, lag: {_lag}";

        HeaderWithProtocol _head = HeaderWithProtocol.Empty();
        Constants _constants = Constants.Empty();

        protected override async Task OnTick(CancellationToken cancellationToken)
        {
            var head = await _node.GetAsync<HeaderWithProtocol>(_lag != 0
                ? $"chains/main/blocks/head~{_lag}/header"
                : "chains/main/blocks/head/header");

            Notify(head);

            if (head.Hash != _head.Hash)
            {
                if (head.Protocol != _head.Protocol)
                    _constants = await _node.GetAsync<Constants>($"chains/main/blocks/{head.Level}/context/constants");

                _head = head;

                var now = DateTime.UtcNow;
                var nextBlockTime = head.Timestamp.AddSeconds(_constants.MinBlockDelay * (_lag + 1));
                await Task.Delay(nextBlockTime > now ? nextBlockTime - now : TimeSpan.FromMicroseconds(_period), cancellationToken);
            }
            else
            {
                await Task.Delay(_period, cancellationToken);
            }
        }
    }

    class HeaderWithProtocol : Header
    {
        [JsonPropertyName("protocol")]
        public required string Protocol { get; set; }

        public static new HeaderWithProtocol Empty() => new()
        {
            Protocol = string.Empty,
            Predecessor = string.Empty,
            Hash = string.Empty,
            Level = -1,
            Timestamp = DateTime.MinValue,
        };
    }
}
