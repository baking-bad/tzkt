using System;

namespace Tzkt.Api.Models
{
    public class BakingRight
    {
        /// <summary>
        /// Type of the right:
        /// - `baking` - right to bake (produce) a block;
        /// - `endorsing` - right to endorse (validate) a block.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Cycle on which the right can be realized.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Level at which a block must be baked or an endorsement must be sent.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Time (estimated, in case of future rights) when a block must be baked or an endorsement must be sent.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Round (0 - ∞) at which the baker can propose/produce a block.
        /// If a baker at round  `0` doesn't produce a block within the given time interval, then the right goes to a baker at round` 1`, etc.
        /// For `endorsing` rights this field is always `null`.
        /// </summary>
        public int? Round { get; set; }

        /// <summary>
        /// Number of slots (1 - 32) to be endorsed. For `baking` rights this field is always `null`.
        /// </summary>
        public int? Slots { get; set; }

        /// <summary>
        /// Baker to which baking or endorsing right has been given.
        /// </summary>
        public Alias Baker { get; set; }

        /// <summary>
        /// Status of the baking or endorsing right:
        /// - `future` - the right is not realized yet;
        /// - `realized` - the right was successfully realized;
        /// - `missed` - the right was not realized.
        /// </summary>
        public string Status { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public int? Priority => Round;
        #endregion
    }
}
