namespace Tzkt.Api
{
    public class TokenBalanceFilter
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by account address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter account { get; set; }

        /// <summary>
        /// Filter by token.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TokenInfoFilter token { get; set; }

        /// <summary>
        /// Filter by balance.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public NatParameter balance { get; set; }

        /// <summary>
        /// Filter by level of the block where the balance was first changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the balance was first changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the balance was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the balance was last changed.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }
    }
}
