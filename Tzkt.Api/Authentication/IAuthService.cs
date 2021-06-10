using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tzkt.Api.Utils;
using static Tzkt.Api.Utils.AuthConfig.Methods;

namespace Tzkt.Api.Authentication
{
    public interface IAuthService
    {
        public bool Authorized(AuthHeaders headers, string json, out string error);

        public bool Authorized(AuthHeaders headers, out string error);
    }
    
    public static class AuthServiceExt
    {
        public static void AddAuthService(this IServiceCollection services, IConfiguration config)
        {
            
            switch (config.GetAuthConfig().Method)
            {
                case Default:
                    services.AddSingleton<IAuthService, DefaultAuthService>();
                    break;
                case PubKey:
                    services.AddSingleton<IAuthService, PubKeyAuthService>();
                    break;
                case Password:
                    services.AddSingleton<IAuthService, PasswordAuthService>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}