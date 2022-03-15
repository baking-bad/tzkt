namespace Tzkt.Api.Services.Cache
{
    public class RawUser : RawAccount
    {
        public bool? Activated { get; set; }
        public string PublicKey { get; set; }
        public bool Revealed { get; set; }
        public int RegisterConstantsCount { get; set; }
        public int SetDepositsLimitsCount { get; set; }
    }
}
