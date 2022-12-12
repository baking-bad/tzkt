using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;

namespace Tzkt.Api.Utils;

public static class MetricsRegistry
{
    public static GaugeOptions CacheHitsGauge = new GaugeOptions
    {
        Name = "Cache Hits Gauge",
        MeasurementUnit = Unit.Bytes,
    };

    public static GaugeOptions CacheMissGauge = new GaugeOptions
    {
        Name = "Cache Miss Gauge",
        MeasurementUnit = Unit.Bytes,
    };

    public static GaugeOptions CacheUsageGauge = new GaugeOptions
    {
        Name = "Cache Usage Rate",
        MeasurementUnit = Unit.Bytes,
    };

    public static CounterOptions WebsocketConnectionsCounter = new CounterOptions
    {
        Name = "Websocket connections",
        MeasurementUnit = Unit.Bytes,
    };
}