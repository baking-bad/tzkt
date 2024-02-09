using System.Collections.Generic;

namespace Mvkt.Api.Services.Auth
{
    public class AuthUser
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string PubKey { get; set; }
        public List<AccessRights> Rights { get; set; }
    }
}