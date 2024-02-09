using Microsoft.Extensions.Configuration;

namespace Mvkt.Api.Services
{
    public class HomeConfig
    {
        public bool Enabled { get; set; } = false;
        public int UpdatePeriod { get; set; } = 10;
    }

    public static class HomeConfigExt
    {
        public static HomeConfig GetHomeConfig(this IConfiguration config)
        {
            return config.GetSection("Home")?.Get<HomeConfig>() ?? new HomeConfig();
        }
    }
}