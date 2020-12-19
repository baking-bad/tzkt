using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables("TZKT_");
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddEnvironmentVariables(prefix: "TZKT_");
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

                if (db.Database.GetAppliedMigrations().Any() &&
                    db.Database.GetAppliedMigrations().First() != db.Database.GetMigrations().First())
                {
                    attempt = 10;
                    throw new Exception($"can't migrate database. Please, restore it from the snapshot with the latest version.");
                }

                var pending = db.Database.GetPendingMigrations();
                if (pending.Any())
                {
                    logger.LogWarning($"{pending.Count()} database migrations were found. Applying migrations...");
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
