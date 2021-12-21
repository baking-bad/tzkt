namespace Tzkt.Api.Services.Auth
{
    public class AuthUser
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string PubKey { get; set; }
        public AuthRights AuthRights { get; set; } = AuthRights.None;
    }
}