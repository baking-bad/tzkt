using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tzkt.Api.Services.Output
{
    public static class OutputCachingExtension
    {
        public static void AddOutputCaching(this IServiceCollection services)
        {
            services.TryAddSingleton<OutputCachingService>();
            services.TryAddSingleton<OutputCacheKeysProvider>();
        }
    }
}