using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class  BakingOperation : Operation
    {
        public override string Type => OpTypes.Baking;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override int Id { get; set; }

        /// <summary>
        /// The height of the block, from the genesis block
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The datetime at which the block is claimed to have been created (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Block hash
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Information about a baker (validator), produced the block
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// The position in the priority list of delegates at which the block was baked
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Reward of the baker for producing the block (micro tez)
        /// </summary>
        public long Reward { get; set; }

        /// <summary>
        /// Total fee paid by all operations, included in the block
        /// </summary>
        public long Fees { get; set; }
    }
}
