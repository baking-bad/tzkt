using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Api.Services.Auth
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
                case AuthConfig.Methods.Default:
                    services.AddSingleton<IAuthService, DefaultAuth>();
                    break;
                case AuthConfig.Methods.PubKey:
                    services.AddSingleton<IAuthService, PubKeyAuth>();
                    break;
                case AuthConfig.Methods.Password:
                    services.AddSingleton<IAuthService, PasswordAuth>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}