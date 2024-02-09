﻿using System;

namespace Mvkt.Api.Models
{
    public class DoublePreendorsingOperation : Operation
    {
        /// <summary>
        /// Type of the operation, `double_preendorsing`
        /// </summary>
        public override string Type => OpTypes.DoublePreendorsing;

        /// <summary>
        /// Unique ID of the operation, stored in the MvKT indexer database
        /// </summary>
        public override long Id { get; set; }

        /// <summary>
        /// Height of the block from the genesis block, in which the operation was included
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
        /// Height of the block from the genesis, at which double preendorsing occurred 
        /// </summary>
        public int AccusedLevel { get; set; }

        /// <summary>
        /// Height of the block from the genesis, at which the offender was slashed
        /// </summary>
        public int SlashedLevel { get; set; }

        /// <summary>
        /// Information about the baker, produced the block, in which the accusation was included
        /// </summary>
        public Alias Accuser { get; set; }

        /// <summary>
        /// Reward of the baker, produced the block, in which the accusation was included
        /// </summary>
        public long Reward { get; set; }

        /// <summary>
        /// Information about the baker, accused for producing two different preendorsements at the same level
        /// </summary>
        public Alias Offender { get; set; }

        /// <summary>
        /// Amount slashed from baker's own staked balance
        /// </summary>
        public long LostStaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's own unstaked balance
        /// </summary>
        public long LostUnstaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's external staked balance
        /// </summary>
        public long LostExternalStaked { get; set; }

        /// <summary>
        /// Amount slashed from baker's external unstaked balance
        /// </summary>
        public long LostExternalUnstaked { get; set; }

        /// <summary>
        /// Amount lost due to inaccuracy of the economic protocol introduced in Oxford.
        /// </summary>
        public long RoundingLoss { get; set; }

        #region injecting
        /// <summary>
        /// Injected historical quote at the time of operation
        /// </summary>
        public QuoteShort Quote { get; set; }
        #endregion

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long OffenderLoss => LostStaked + LostUnstaked + LostExternalStaked + LostExternalUnstaked + RoundingLoss;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long AccuserReward => Reward;
        #endregion
    }
}
