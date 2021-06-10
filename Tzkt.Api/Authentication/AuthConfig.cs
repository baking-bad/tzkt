using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Tzkt.Api.Utils
{
    public class AuthConfig
    {
        public enum Methods
        {
            Default,
            PubKey,
            Password
        }

        public Methods Method { get; set; } = Methods.Default;
        public int NonceLifetime { get; set; } = 100;
        public List<Admin> Admins { get; set; } = new();
    }

    public static class CacheConfigExt
    {
        public static AuthConfig GetAuthConfig(this IConfiguration config)
        {
            return config.GetSection("Authentication")?.Get<AuthConfig>() ?? new AuthConfig();
        }
    }
}