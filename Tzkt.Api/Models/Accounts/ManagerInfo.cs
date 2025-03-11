namespace Tzkt.Api.Models
{
    public class ManagerInfo
    {
        /// <summary>
        /// Name of the project behind the account or account description
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Public key hash of the account
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Base58 representation of account's public key, revealed by the account
        /// </summary>
        public required string PublicKey { get; set; }
    }
}
