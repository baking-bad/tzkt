using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tezzycat.Data;
using Tezzycat.Sync.Services;

namespace Tezzycat.Sync
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("TZKT_Environment")
                ?? EnvironmentName.Production;

            var builder = new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile(
                        $"appsettings.json",
                        optional: false,
                        reloadOnChange: false);

                    config.AddJsonFile(
                        $"appsettings.{environment}.json",
                        optional: true,
                        reloadOnChange: false);

                    config.AddEnvironmentVariables("TZKT_");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddEntityFrameworkNpgsql()
                        .AddDbContext<SyncContext>(options =>
                        {
                            options.UseNpgsql(
                                hostContext.Configuration.GetConnectionString("DefaultConnection"));
                        });

                    services.AddMemoryCache();

                    services.AddTezosNode();
                    services.AddTezosProtocols();
                    services.AddHostedService<Observer>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConfiguration(
                        hostContext.Configuration.GetSection("Logging"));

                    logging.AddConsole();
                    logging.AddDebug();
                });

            await builder.RunConsoleAsync();
        }
    }
}
