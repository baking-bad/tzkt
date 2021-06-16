using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Utils;
using Tzkt.Api.Utils;

namespace Tzkt.Api.Services.Auth
{
    public class PasswordAuth : IAuthService
    {
        private readonly AuthConfig Config;

        public PasswordAuth(IConfiguration config)
        {
            Config = config.GetAuthConfig();
        }
        
        public bool Authorized(AuthHeaders headers, string json, out string error)
        {
            return Authorized(headers, out error);
        }
        
        public bool Authorized(AuthHeaders headers, out string error)
        {
            error = null;
            
            if (string.IsNullOrWhiteSpace(headers.User))
            {
                error = $"The X-TZKT-USER header is required";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(headers.Password))
            {
                error = $"The X-TZKT-PASSWORD header is required";
                return false;
            }
                
            if(Config.Admins.All(x => x.Username != headers.User))
            {
                error = $"User {headers.User} doesn't exist";
                return false;
            }
            
            if (Config.Admins.FirstOrDefault(u => u.Username == headers.User)?.Password != headers.Password)
            {
                error = $"Invalid password";
                return false;
            }
            
            return true;
        }
    }
}