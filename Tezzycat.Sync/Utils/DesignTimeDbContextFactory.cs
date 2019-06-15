using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Tezzycat.Data;

namespace Tezzycat.Sync
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SyncContext>
    {
        public SyncContext CreateDbContext(string[] args)
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

            var builder = new DbContextOptionsBuilder<SyncContext>();
            builder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            return new SyncContext(builder.Options);
        }
    }
}
