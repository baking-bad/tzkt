using System;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tzkt.Api.Services.Auth;
using Tzkt.Data;

namespace Tzkt.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
                
            if (host.Check())
            {
                host.Init().Run();
            }
            
            return;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((host, appConfig) =>
                {
                    appConfig.Sources.Clear();
                    appConfig.AddJsonFile("appsettings.json", true);
                    appConfig.AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true);
                    appConfig.AddEnvironmentVariables();
                    appConfig.AddEnvironmentVariables("TZKT_API_");
                    appConfig.AddCommandLine(args);
                })
                .ConfigureLogging(logConfig =>
                {
                    logConfig.ClearProviders();
                    logConfig.AddConsole();
                });
    }

    static class IHostExt
    {
        public static IHost Init(this IHost host, int attempt = 0)
        {
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var db = scope.ServiceProvider.GetRequiredService<TzktContext>();

            logger.LogInformation("Version {verion}",
                Assembly.GetExecutingAssembly().GetName().Version.ToString());

            try
            {
                logger.LogInformation("Initialize database");

                var migrations = db.Database.GetMigrations().ToList();
                var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
                for (int i = 0; i < Math.Min(migrations.Count, appliedMigrations.Count); i++)
                {
                    if (migrations[i] != appliedMigrations[i])
                    {
                        attempt = 30;
                        throw new Exception($"API and DB schema have incompatible versions. Drop the DB and restore it from the appropriate snapshot.");
                    }
                }

                if (appliedMigrations.Count > migrations.Count)
                {
                    attempt = 30;
                    throw new Exception($"API version seems older than version of the DB schema. Update the API to the newer version.");
                }

                if (appliedMigrations.Count < migrations.Count)
                    throw new Exception($"{migrations.Count - appliedMigrations.Count} database migrations are pending.");

                var state = db.AppState.Single();
                if (state.Level < 1)
                    throw new Exception("database is empty, at least two blocks are needed.");

                logger.LogInformation("Database initialized");
                return host;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"Failed to initialize database: {ex.Message}");
                if (attempt >= 30) throw;
                Thread.Sleep(1000);

                return host.Init(++attempt);
            }
        }
        
        public static bool Check(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                config.ValidateAuthConfig(logger);
            }
            catch (ConfigurationException ex)
            {
                logger.LogError($"Configuration validation failed {ex}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"Configuration validation failed {ex}");
                return false;
            }

                
            return true;
        }
    }
}
