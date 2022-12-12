using App.Metrics;
using App.Metrics.Timer;

namespace Tzkt.Sync;

public static class MetricsRegistry
{
    public static TimerOptions BlockApplyTimer = new TimerOptions
    {
        Name = "Block apply timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static TimerOptions BlockWaitingTimer = new TimerOptions
    {
        Name = "Block waiting timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static TimerOptions BlockCommitDbTransactionTimer = new TimerOptions
    {
        Name = "Block commit DB transaction timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static TimerOptions ReorgTimer = new TimerOptions
    {
        Name = "Reorg timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };

    public static TimerOptions DiagnosticsTimer = new TimerOptions
    {
        Name = "Diagnostics timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };

    public static TimerOptions ValidationTimer = new TimerOptions
    {
        Name = "Validation timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static TimerOptions RpcBlockRequestTimer = new TimerOptions
    {
        Name = "Rpc block request timer",
        MeasurementUnit = Unit.Requests,
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };

}