using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Timer;

namespace Tzkt.Sync
{
    static class MetricsRegistry
    {
        #region observe
        public static readonly HistogramOptions BlockAppearanceDelay = new()
        {
            Context = "Observe",
            Name = "Block appearance delay"
        };
        #endregion

        #region apply
        public static readonly TimerOptions ApplyBlockTime = new()
        {
            Context = "Apply block",
            Name = "Apply block time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RpcResponseTime = new()
        {
            Context = "Apply block",
            Name = "Rpc response time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions CacheWarmUpTime = new()
        {
            Context = "Apply block",
            Name = "Cache warm up time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions ValidationTime = new()
        {
            Context = "Apply block",
            Name = "Validation time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions ProcessingTime = new()
        {
            Context = "Apply block",
            Name = "Processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions DiagnosticsTime = new()
        {
            Context = "Apply block",
            Name = "Diagnostics time",
            DurationUnit = TimeUnit.Milliseconds, 
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions SaveChangesTime = new()
        {
            Context = "Apply block",
            Name = "Save changes time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions PostProcessingTime = new()
        {
            Context = "Apply block",
            Name = "Post-processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions QuotesProcessingTime = new()
        {
            Context = "Apply block",
            Name = "Quotes processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        #endregion

        #region revert
        public static readonly TimerOptions RevertBlockTime = new()
        {
            Context = "Revert block",
            Name = "Revert block time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RevertQuotesProcessingTime = new()
        {
            Context = "Revert block",
            Name = "Quotes processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RevertPostProcessingTime = new()
        {
            Context = "Revert block",
            Name = "Post-processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RevertProcessingTime = new()
        {
            Context = "Revert block",
            Name = "Processing time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RevertDiagnosticsTime = new()
        {
            Context = "Revert block",
            Name = "Diagnostic time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        public static readonly TimerOptions RevertSaveChangesTime = new()
        {
            Context = "Revert block",
            Name = "Save changes time",
            DurationUnit = TimeUnit.Milliseconds,
            RateUnit = TimeUnit.Milliseconds
        };
        #endregion
    }
}