using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Tzkt.Data;

namespace Tzkt.Sync
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TzktContext>
    {
        public TzktContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("TZKT_Environment")
                ?? EnvironmentName.Production;

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                        $"appsettings.json",
                        optional: false,
                        reloadOnChange: false)
                .AddJsonFile(
                    $"appsettings.{environment}.json",
                    optional: true,
                    reloadOnChange: false)

                .AddEnvironmentVariables("TZKT_")
                .Build();

            var builder = new DbContextOptionsBuilder<TzktContext>();
            builder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            return new TzktContext(builder.Options);
        }
    }
}
