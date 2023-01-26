using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.Prometheus;
using Tzkt.Data;
using Tzkt.Sync;
using Tzkt.Sync.Services;
using Tzkt.Sync.Services.Domains;

var builder = WebApplication.CreateBuilder(args);

#region configuration
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables("TZKT_SYNC_");
builder.Configuration.AddCommandLine(args);
#endregion

#region logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#endregion

#region services
builder.Services.AddDbContext<TzktContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCaches();
builder.Services.AddTezosNode();
builder.Services.AddTezosProtocols();
builder.Services.AddQuotes(builder.Configuration);
builder.Services.AddHostedService<Observer>();

if (builder.Configuration.GetDomainsConfig().Enabled)
    builder.Services.AddHostedService<DomainsService>();

if (builder.Configuration.GetContractMetadataConfig().Enabled)
    builder.Services.AddHostedService<ContractMetadata>();

if (builder.Configuration.GetTokenMetadataConfig().Enabled)
    builder.Services.AddHostedService<TokenMetadata>();

var healthChecks = builder.Configuration.GetHealthChecksConfig();
if (healthChecks.Enabled)
{
    builder.Services.AddHealthChecks()
        .AddCheck<DumbHealthCheck>(nameof(DumbHealthCheck));

    builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
    builder.Services.Configure<HealthCheckPublisherOptions>(config =>
    {
        config.Delay = TimeSpan.FromSeconds(healthChecks.Delay);
        config.Period = TimeSpan.FromSeconds(healthChecks.Period);
    });
}

builder.Services.AddMetrics(options =>
{
    options.Configuration.ReadFrom(builder.Configuration);
    options.OutputMetrics.AsPrometheusPlainText();
    options.OutputMetrics.AsPrometheusProtobuf();
});

builder.Services.AddMetricsEndpoints(builder.Configuration, options =>
{
    options.MetricsEndpointOutputFormatter = new MetricsPrometheusProtobufOutputFormatter();
    options.MetricsTextEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
});
#endregion

var app = builder.Build();

#region init
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Version {version}", Assembly.GetExecutingAssembly().GetName().Version);

while (true)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TzktContext>();
    try
    {
        logger.LogInformation("Initialize database...");

        var migrations = db.Database.GetMigrations().ToList();
        var applied = db.Database.GetAppliedMigrations().ToList();

        for (int i = 0; i < Math.Min(migrations.Count, applied.Count); i++)
        {
            if (migrations[i] != applied[i])
            {
                logger.LogError("Initialization failed: indexer and DB schema have incompatible versions. Drop the DB and restore it from the appropriate snapshot.");
                return 1;
            }
        }

        if (applied.Count > migrations.Count)
        {
            logger.LogError("Initialization failed: indexer version is out of date. Update the indexer to the newer version.");
            return 2;
        }

        if (applied.Count < migrations.Count)
        {
            logger.LogWarning("{cnt} pending migrations. Migrate database...", migrations.Count - applied.Count);
            db.Database.SetCommandTimeout(0);
            db.Database.Migrate();
        }

        logger.LogInformation("Database initialized");
        break;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Initialization failed. Let's try again.");
        Thread.Sleep(3000);
        continue;
    }
}
#endregion

#region middleware
app.UseMetricsEndpoint();
app.UseMetricsTextEndpoint();
app.MapGet("/version", () => Assembly.GetExecutingAssembly().GetName().Version);
#endregion

app.Run();

return 0;
