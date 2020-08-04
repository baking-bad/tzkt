using System;

namespace Tzkt.Api.Models
{
    public class HistoricalBalance
    {
        /// <summary>
        /// Height of the block at which the account balance was calculated
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Timestamp of the block at which the account balance was calculated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Historical balance
        /// </summary>
        public long Balance { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of balance snapshot
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion
    }
}
