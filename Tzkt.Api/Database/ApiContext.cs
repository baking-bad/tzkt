using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data
{
    public class ApiContext : TzktContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
    }
}
