using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;

namespace Tzkt.Api.Utils
{
    static class MetricsRegistry
    {
        public static CounterOptions ResponseCacheCalls = new()
        {
            Name = "Response Cache Calls",
            MeasurementUnit = Unit.Calls,
            ResetOnReporting = true, 
        };

        public static GaugeOptions ResponseCacheSize = new()
        {
            Name = "Response Cache Size",
            MeasurementUnit = Unit.Bytes,
        };

        public static GaugeOptions WebsocketConnections = new()
        {
            Name = "Websocket connections",
            MeasurementUnit = Unit.Connections,
        };

        public static MetricTags ResponseCacheHit = new("cache", "hit");
        public static MetricTags ResponseCacheMiss = new("cache", "miss");
    }
}