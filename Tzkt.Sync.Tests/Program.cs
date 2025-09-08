using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.Prometheus;
using Netezos.Rpc;
using Tzkt.Data;
using Tzkt.Sync.Services;
using Tzkt.Sync.Tests.Database;

namespace Tzkt.Sync.Tests
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            #region configuration
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("settings.json", true);
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

            builder.Services.AddCache(builder.Configuration);
            builder.Services.AddTezosNode();
            builder.Services.AddTezosProtocols();
            builder.Services.AddSingleton<IQuoteProvider, DefaultQuotesProvider>();
            builder.Services.AddScoped<QuotesService>();

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
            var config = builder.Configuration.GetSection("Tests").Get<TestsConfig>() ?? new();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Version {version}", AssemblyInfo.Version);

            try
            {
                InitDb(app);

                if (config.RunIndexer)
                {
                    logger.LogInformation("Syncing up with the node");
                    await Indexer.RunAsync(app, default);
                }

                using var scope = app.Services.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<TzktContext>();
                var rpcEndpoint = builder.Configuration.GetSection("TezosNode").GetValue<string>("Endpoint")
                    ?? throw new Exception("TezosNode.Endpoint is not specified in the configuration");
                var rpc = new TezosRpc(rpcEndpoint, 60);

                logger.LogInformation("Run AppStateTests");
                await AppStateTests.RunAsync(db, rpc);

                logger.LogInformation("Run StatisticsTests");
                await StatisticsTests.RunAsync(db);

                logger.LogInformation("Tests passed");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tests failed");
                return 1;
            }
        }

        static void InitDb(IHost app, int attempt = 0)
        {
            using var scope = app.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<TzktContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Initialize database");

                var migrations = db.Database.GetMigrations().ToList();
                var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
                for (int i = 0; i < Math.Min(migrations.Count, appliedMigrations.Count); i++)
                {
                    if (migrations[i] != appliedMigrations[i])
                    {
                        attempt = 10;
                        throw new Exception("Indexer and DB schema have incompatible versions");
                    }
                }

                if (appliedMigrations.Count > migrations.Count)
                {
                    attempt = 10;
                    throw new Exception("Indexer version seems older than version of the DB schema");
                }

                if (appliedMigrations.Count < migrations.Count)
                {
                    logger.LogWarning("{cnt} migrations can be applied. Migrating database...", migrations.Count - appliedMigrations.Count);
                    db.Database.SetCommandTimeout(0);
                    db.Database.Migrate();
                }

                logger.LogInformation("Database initialized");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to initialize database");
                if (attempt >= 10) throw;
                Thread.Sleep(1000);

                InitDb(app, ++attempt);
            }
        }
    }
}