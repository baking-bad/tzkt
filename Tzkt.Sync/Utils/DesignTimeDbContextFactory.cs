using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tzkt.Data;

namespace Tzkt.Sync
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TzktContext>
    {
        public TzktContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    $"appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var builder = new DbContextOptionsBuilder<TzktContext>();
            builder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            return new TzktContext(builder.Options);
        }
    }
}
