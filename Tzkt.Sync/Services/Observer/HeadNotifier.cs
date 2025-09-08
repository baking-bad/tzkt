using System.Text.Json.Serialization;
using Tzkt.Sync.Services.Observer.Notifiers;

namespace Tzkt.Sync.Services.Observer
{
    abstract class HeadNotifier(ILogger _logger)
    {
        public event OnHeadEventHandler? OnHead;

        protected abstract string Parameters { get; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Head notifier started ({params})", Parameters);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await OnTick(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch head from node");
                    await Task.Delay(5000, cancellationToken);
                }
            }

            _logger.LogInformation("Head notifier stopped");
        }

        protected abstract Task OnTick(CancellationToken cancellationToken);

        protected void Notify(Header head)
        {
            OnHead?.Invoke(head);
        }

        public static HeadNotifier Create(ObserverConfig config, TezosNode node, ILogger logger)
        {
            if (config.Method == "streaming")
            {
                return config.Lag != 0
                    ? new LaggedStreamingHeadNotifier(config.Lag, node, logger)
                    : new StreamingHeadNotifier(node, logger);
            }
            else
            {
                return config.Debounce
                    ? new DebouncedPollingHeadNotifier(config.Lag, config.Period, node, logger)
                    : new PollingHeadNotifier(config.Lag, config.Period, node, logger);
            }
        }
    }

    delegate void OnHeadEventHandler(Header head);

    class Header
    {
        [JsonPropertyName("predecessor")]
        public required string Predecessor { get; set; }

        [JsonPropertyName("hash")]
        public required string Hash { get; set; }

        [JsonPropertyName("level")]
        public required int Level { get; set; }

        [JsonPropertyName("timestamp")]
        public required DateTime Timestamp { get; set; }

        public static Header Empty() => new()
        {
            Predecessor = string.Empty,
            Hash = string.Empty,
            Level = -1,
            Timestamp = DateTime.MinValue,
        };
    }

    class Constants
    {
        [JsonPropertyName("minimal_block_delay")]
        public int MinBlockDelay { get; set; }

        public static Constants Empty() => new()
        {
            MinBlockDelay = 0,
        };
    }
}
