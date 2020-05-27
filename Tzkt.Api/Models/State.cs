using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class State
    {
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
        /// The datetime at which the last block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

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
    }
}
