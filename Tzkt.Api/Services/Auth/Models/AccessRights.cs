namespace Tzkt.Api.Services.Auth
{
    public class AccessRights
    {
        public required string Table { get; set; }
        public string? Section { get; set; }
        public Access Access {get;set;}
    }

    public enum Access
    {
        None,
        Read,
        Write
    }
}
