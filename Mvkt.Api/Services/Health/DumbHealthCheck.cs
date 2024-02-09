﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mvkt.Api.Services
{
    class DumbHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
