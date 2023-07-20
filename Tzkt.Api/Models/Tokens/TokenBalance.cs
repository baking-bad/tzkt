namespace Tzkt.Api.Models
{
    public class TokenBalance
    {
        /// <summary>
        /// Internal TzKT id.  
        /// **[sortable]**
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Owner account.  
        /// Click on the field to expand more details.
        /// </summary>
        public Alias Account { get; set; }

        /// <summary>
        /// Token info.  
        /// Click on the field to expand more details.
        /// </summary>
        public TokenInfo Token { get; set; }

        /// <summary>
        /// Balance (raw value, not divided by `decimals`).  
        /// **[sortable]**
        /// </summary>
        public string Balance { get; set; }

        /// <summary>
        /// Balance value in mutez, based on the current token price.  
        /// **[sortable]**
        /// </summary>
        public decimal? BalanceValue { get; set; } // TODO: change to BigInteger, when migrating to .net7

        /// <summary>
        /// Total number of transfers, affecting the token balance.  
        /// **[sortable]**
        /// </summary>
        public int TransfersCount { get; set; }

        /// <summary>
        /// Level of the block where the token balance was first changed.  
        /// **[sortable]**
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token balance was first changed.
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the block where the token balance was last changed.  
        /// **[sortable]**
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the token balance was last changed.
        /// </summary>
        public DateTime LastTime { get; set; }
    }
}
