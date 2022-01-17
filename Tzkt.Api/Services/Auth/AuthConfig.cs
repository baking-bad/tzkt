using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Netezos.Keys;

namespace Tzkt.Api.Services.Auth
{
    public class AuthConfig
    {
        public AuthMethod Method { get; set; } = AuthMethod.None;
        public int NonceLifetime { get; set; } = 10;
        public List<AuthUser> Users { get; set; } = new();
    }

    public static class AuthConfigExt
    {
        public static AuthConfig GetAuthConfig(this IConfiguration config)
        {
            return config.GetSection("Authentication")?.Get<AuthConfig>();
        }

        public static void ValidateAuthConfig(this IConfiguration config, ILogger<Program> logger)
        {
            try
            {
                var authConfig = config.GetAuthConfig();
                if (authConfig == null)
                    return;
                
                foreach (var user in authConfig.Users)
                {
                    var key = PubKey.FromBase58(user.PubKey);
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(ex.Message);
            }
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message)
            : base($"Bad configuration: {message}") { }
    }
}