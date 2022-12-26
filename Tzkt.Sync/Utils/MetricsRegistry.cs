using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Timer;

namespace Tzkt.Sync;

static class MetricsRegistry
{
    public static readonly TimerOptions CacheWarmUpTimer = new TimerOptions
    {
        Name = "Cache warm up timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions BlockProcessingTimer = new TimerOptions
    {
        Name = "Block processing timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly HistogramOptions BlockAppearanceDelay = new HistogramOptions()
    {
        Name = "Block appearance delay"
    };
    public static readonly TimerOptions DiagnosticsTimer = new TimerOptions
    {
        Name = "Diagnostics timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions DbSaveTimer = new TimerOptions
    {
        Name = "DB save timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions PostProcessingTimer = new TimerOptions
    {
        Name = "Post processing timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions QuotesProcessingTimer = new TimerOptions
    {
        Name = "Quotes processing timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };

    public static readonly TimerOptions ValidationTimer = new TimerOptions
    {
        Name = "Validation timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions RpcBlockRequestTimer = new TimerOptions
    {
        Name = "Rpc block request timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions QuotesRevertTimer = new TimerOptions
    {
        Name = "Quotes revert timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions PostchangesRevertTimer = new TimerOptions
    {
        Name = "Post-changes revert timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions BlockRevertTimer = new TimerOptions
    {
        Name = "Block revert timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions DiagnosticOfRevertTimer = new TimerOptions
    {
        Name = "Diagnostic of revert timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
    public static readonly TimerOptions RevertDbSaveTimer = new TimerOptions
    {
        Name = "Revert DB save timer",
        DurationUnit = TimeUnit.Milliseconds,
        RateUnit = TimeUnit.Milliseconds
    };
}