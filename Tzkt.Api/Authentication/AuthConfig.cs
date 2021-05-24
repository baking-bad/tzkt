using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Utils
{
    public class AuthConfig
    {
        public bool Enabled { get; set; } = true;
        public int NonceLifetime { get; set; } = 100;
    }

    public static class CacheConfigExt
    {
        public static AuthConfig GetAuthConfig(this IConfiguration config)
        {
            return config.GetSection("Authentication")?.Get<AuthConfig>() ?? new AuthConfig();
        }
    }
}