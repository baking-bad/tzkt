using System;

namespace Tzkt.Api.Models
{
    public class State
    {
        /// <summary>
        /// Alias name of the chain (or "private" if it's not on the list of known chains)
        /// </summary>
        public string Chain { get; set; }

        /// <summary>
        /// Unique identifier of the chain
        /// </summary>
        public string ChainId { get; set; }

        /// <summary>
        /// Current cycle
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// The height of the last block from the genesis block
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// Block hash
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// Current protocol hash
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Next block protocol hash
        /// </summary>
        public string NextProtocol { get; set; }

        /// <summary>
        /// The datetime at which the last block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current voting epoch index, starting from zero
        /// </summary>
        public int VotingEpoch { get; set; }

        /// <summary>
        /// Current voting period index, starting from zero
        /// </summary>
        public int VotingPeriod { get; set; }

        /// <summary>
        /// The height of the last known block from the genesis block
        /// </summary>
        public int KnownLevel { get; set; }
        
        /// <summary>
        /// The datetime of last TzKT indexer synchronization (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime LastSync { get; set; }
        
        /// <summary>
        /// State of TzKT indexer synchronization
        /// </summary>
        public bool Synced => KnownLevel == Level;

        /// <summary>
        /// The height of the block where quotes were updated last time
        /// </summary>
        public int QuoteLevel { get; set; }

        /// <summary>
        /// Last known XTZ/BTC price
        /// </summary>
        public double QuoteBtc { get; set; }

        /// <summary>
        /// Last known XTZ/EUR price
        /// </summary>
        public double QuoteEur { get; set; }

        /// <summary>
        /// Last known XTZ/USD price
        /// </summary>
        public double QuoteUsd { get; set; }

        /// <summary>
        /// Last known XTZ/CNY price
        /// </summary>
        public double QuoteCny { get; set; }

        /// <summary>
        /// Last known XTZ/JPY price
        /// </summary>
        public double QuoteJpy { get; set; }

        /// <summary>
        /// Last known XTZ/KRW price
        /// </summary>
        public double QuoteKrw { get; set; }

        /// <summary>
        /// Last known XTZ/ETH price
        /// </summary>
        public double QuoteEth { get; set; }

        /// <summary>
        /// Last known XTZ/GBP price
        /// </summary>
        public double QuoteGbp { get; set; }
    }
}
