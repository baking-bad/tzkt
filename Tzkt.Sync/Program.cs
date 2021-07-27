using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tzkt.Data;
using Tzkt.Sync.Services;

namespace Tzkt.Sync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Init().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((host, appConfig) =>
                {
                    appConfig.Sources.Clear();
                    appConfig.AddJsonFile("appsettings.json", true);
                    appConfig.AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true);
                    appConfig.AddEnvironmentVariables();
                    appConfig.AddEnvironmentVariables("TZKT_SYNC_");
                    appConfig.AddCommandLine(args);
                })
                .ConfigureLogging(logConfig =>
                {
                    logConfig.ClearProviders();
                    logConfig.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<TzktContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddCaches();
                    services.AddTezosNode();
                    services.AddTezosProtocols();
                    services.AddQuotes(hostContext.Configuration);
                    services.AddHostedService<Observer>();

                    #region healh checks
                    var healthChecks = hostContext.Configuration.GetHealthChecksConfig();
                    if (healthChecks.Enabled)
                    {
                        services.AddHealthChecks()
                            .AddCheck<DumbHealthCheck>(nameof(DumbHealthCheck));

                        services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
                        services.Configure<HealthCheckPublisherOptions>(config =>
                        {
                            config.Delay = TimeSpan.FromSeconds(healthChecks.Delay);
                            config.Period = TimeSpan.FromSeconds(healthChecks.Period);
                        });
                    }
                    #endregion
                });
    }

    static class IHostExt
    {
        public static IHost Init(this IHost host, int attempt = 0)
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var db = scope.ServiceProvider.GetRequiredService<TzktContext>();

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
                        throw new Exception($"indexer and DB schema have incompatible versions. Drop the DB and restore it from the appropriate snapshot.");
                    }
                }

                if (appliedMigrations.Count > migrations.Count)
                {
                    attempt = 10;
                    throw new Exception($"indexer version seems older than version of the DB schema. Update the indexer to the newer version.");
                }

                if (appliedMigrations.Count < migrations.Count)
                {
                    logger.LogWarning($"{migrations.Count - appliedMigrations.Count} migrations can be applied. Migrating database...");
                    db.Database.Migrate();
                }

                logger.LogInformation("Database initialized");
                return host;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"Failed to initialize database: {ex.Message}");
                if (attempt >= 10) throw;
                Thread.Sleep(1000);

                return host.Init(++attempt);
            }
        }
    }
}
