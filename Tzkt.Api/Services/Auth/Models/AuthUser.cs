namespace Tzkt.Api.Services.Auth
{
    public class AuthUser
    {
        public required string Name { get; set; }
        public string? Password { get; set; }
        public string? PubKey { get; set; }
        public List<AccessRights>? Rights { get; set; }
    }
}