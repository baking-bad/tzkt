﻿using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tzkt.Api.Services
{
    class DumbHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
