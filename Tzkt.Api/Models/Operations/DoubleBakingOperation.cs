using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class DoubleBakingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `double_baking` - is used by bakers to provide evidence of double baking (baking two different blocks at the same height) by a baker
        /// </summary>
        public override string Type => OpTypes.DoubleBaking;

        /// <summary>
        /// Unique ID of the operation, stored in the TzKT indexer database
        /// </summary>
        public override int Id { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, in which the operation was included
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Datetime of the block, in which the operation was included (ISO 8601, e.g. `2020-02-20T02:40:57Z`)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Hash of the block, in which the operation was included
        /// </summary>
        public string Block { get; set; }

        /// <summary>
        /// Hash of the operation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// The height of the block from the genesis block, which was double baked
        /// </summary>
        public int AccusedLevel { get; set; }

        /// <summary>
        /// Information about the baker (delegate), produced the block, in which the operation was included
        /// </summary>
        public Alias Accuser { get; set; }

        /// <summary>
        /// Reward of the baker (delegate), produced the block, in which the operation was included
        /// </summary>
        public long AccuserRewards { get; set; }

        /// <summary>
        /// Information about the baker (delegate), accused for producing two different blocks at the same height
        /// </summary>
        public Alias Offender { get; set; }

        /// <summary>
        /// Amount of frozen security deposit, lost by accused baker (delegate)
        /// </summary>
        public long OffenderLostDeposits { get; set; }

        /// <summary>
        /// Amount of frozen rewards, lost by accused baker (delegate)
        /// </summary>
        public long OffenderLostRewards { get; set; }

        /// <summary>
        /// Amount of frozen fees, lost by accused baker (delegate)
        /// </summary>
        public long OffenderLostFees { get; set; }
    }
}
