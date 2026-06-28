using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class TokenBalanceShortFilter : INormalizable
    {
        /// <summary>
        /// Filter by account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountWithEntrypointParameter? account { get; set; }

        /// <summary>
        /// Filter by account address entrypoint.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Utf8BytesParameter? entrypoint { get; set; }

        /// <summary>
        /// Filter by token.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenInfoFilter token { get; set; } = new();

        /// <summary>
        /// Filter by balance.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter? balance { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("", 
                ("account", account), ("entrypoint", entrypoint), ("token", token), ("balance", balance));
        }
    }
}
