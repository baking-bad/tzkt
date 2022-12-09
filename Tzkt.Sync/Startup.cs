using App.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Sync;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        // To add all available tracking middleware
        app.UseMetricsAllMiddleware();

        // Or to cherry-pick the tracking of interest
        // app.UseMetricsActiveRequestMiddleware();
        // app.UseMetricsErrorTrackingMiddleware();
        // app.UseMetricsPostAndPutSizeTrackingMiddleware();
        // app.UseMetricsRequestTrackingMiddleware();
        // app.UseMetricsOAuth2TrackingMiddleware();
        // app.UseMetricsApdexTrackingMiddleware();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var metrics = AppMetrics.CreateDefaultBuilder()
            .Build();

        services.AddMetrics(metrics);
        services.AddMetricsTrackingMiddleware();
    }
}