using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tzkt.Sync.Services
{
    class HealthCheckPublisher(IConfiguration config) : IHealthCheckPublisher
    {
        readonly string FilePath = config.GetHealthChecksConfig().FilePath ?? "sync.health";

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            if (report.Status == HealthStatus.Healthy)
            {
                using var _ = File.Create(FilePath);
            }
            else if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            return Task.CompletedTask;
        }
    }
}
