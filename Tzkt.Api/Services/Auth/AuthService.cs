using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tzkt.Api.Services.Auth
{
    public interface IAuthService
    {
        public bool TryAuthorize(AuthHeaders headers, out string error);
        public bool TryAuthorize(AuthHeaders headers, string json, out string error);
    }
    
    public static class AuthServiceExt
    {
        public static void AddAuthService(this IServiceCollection services, IConfiguration config)
        {
            switch (config.GetAuthConfig()?.Method)
            {
                case AuthMethod.Password:
                    services.AddSingleton<IAuthService, PasswordAuth>();
                    break;
                case AuthMethod.PubKey:
                    services.AddSingleton<IAuthService, PubKeyAuth>();
                    break;
                default:
                    services.AddSingleton<IAuthService, DefaultAuth>();
                    break;
            }
        }
    }
}