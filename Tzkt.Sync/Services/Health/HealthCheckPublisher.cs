using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tzkt.Sync.Services
{
    class HealthCheckPublisher : IHealthCheckPublisher
    {
        readonly string FilePath;

        public HealthCheckPublisher(IConfiguration config)
        {
            FilePath = config.GetHealthChecksConfig().FilePath ?? "sync.health";
        }

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
