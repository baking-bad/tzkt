using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;

namespace Tzkt.Api.Utils;

static class MetricsRegistry
{
    public static GaugeOptions CacheHitsGauge = new GaugeOptions
    {
        Name = "Cache Hits Gauge",
        MeasurementUnit = Unit.Percent,
    };

    public static GaugeOptions CacheUsageGauge = new GaugeOptions
    {
        Name = "Cache Usage Rate",
        MeasurementUnit = Unit.Percent,
    };

    public static CounterOptions WebsocketConnectionsCounter = new CounterOptions
    {
        Name = "Websocket connections",
        MeasurementUnit = Unit.Connections,
    };
}