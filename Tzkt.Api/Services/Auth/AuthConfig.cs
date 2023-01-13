using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            return config.GetSection("Authentication")?.Get<AuthConfig>() ?? new();
        }

        public static void ValidateAuthConfig(this IServiceProvider services)
        {
            var config = services.GetRequiredService<IConfiguration>().GetAuthConfig();

            if (config.Method < AuthMethod.None || config.Method > AuthMethod.PubKey)
                throw new ConfigurationException("Invalid auth method");

            foreach (var user in config.Users)
            {
                if (user.Name == null)
                    throw new ConfigurationException("Invalid user name");

                if (config.Method == AuthMethod.PubKey)
                {
                    try { _ = PubKey.FromBase58(user.PubKey); }
                    catch { throw new ConfigurationException("Invalid user pubkey"); }
                }
                else if (config.Method == AuthMethod.Password)
                {
                    if (user.Password == null)
                        throw new ConfigurationException("Invalid user password");
                }

                if (user.Rights != null)
                {
                    foreach (var right in user.Rights)
                    {
                        if (right.Access < Access.None || right.Access > Access.Write)
                            throw new ConfigurationException("Invalid user access type");
                    }
                }
            }
        }
    }
}