using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Mvkt.Data;

namespace Mvkt.Sync
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MvktContext>
    {
        public MvktContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    $"appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var builder = new DbContextOptionsBuilder<MvktContext>();
            builder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            return new MvktContext(builder.Options);
        }
    }
}
