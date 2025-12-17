using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mvkt.Sync.Services
{
    public class QuotesSyncService : BackgroundService
    {
        readonly IServiceScopeFactory Services;
        readonly QuotesServiceConfig Config;
        readonly ILogger Logger;

        public QuotesSyncService(IServiceScopeFactory services, IConfiguration config, ILogger<QuotesSyncService> logger)
        {
            Services = services;
            Config = config.GetQuotesServiceConfig();
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Quotes sync service started");

                await Task.Delay(5000, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = Services.CreateScope();
                        var quotesService = scope.ServiceProvider.GetRequiredService<QuotesService>();
                        
                        var processed = await quotesService.SyncBatch();
                        
                        if (processed == 0)
                        {
                            await Task.Delay(5000, stoppingToken);
                        }
                        else
                        {
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to sync quotes batch");
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Quotes sync service crashed");
            }
            finally
            {
                Logger.LogInformation("Quotes sync service stopped");
            }
        }
    }
}

