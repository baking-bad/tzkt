using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class PeriodInfo
    {
        /// <summary>
        /// Voting period index, starting from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Voting epoch index, starting from zero
        /// </summary>
        public int Epoch { get; set; }

        /// <summary>
        /// Kind of the voting period
        /// `proposal` - delegates can submit protocol amendment proposals using the proposal operation
        /// `exploration` -  bakers (delegates) may vote on the top-ranked proposal from the previous Proposal Period using the ballot operation
        /// `testing` - If the proposal is approved in the Exploration Period, the Testing Period begins with a testnet
        /// fork that runs in parallel to the main network for 48 hours to test a correct migration of the context
        /// `promotion` - delegates can cast one vote to promote or not the tested proposal using the ballot operation
        /// Learn more: https://tezos.gitlab.io/whitedoc/voting.html
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// The height of the block in which the period starts
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// The height of the block in which the period ends
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// **DEPRECATED**. Use `index` instead.
        /// </summary>
        public int Id => Index;

        /// <summary>
        /// **DEPRECATED**. Use `firstLevel` instead.
        /// </summary>
        public int StartLevel => FirstLevel;

        /// <summary>
        /// **DEPRECATED**. Use `lastLevel` instead.
        /// </summary>
        public int EndLevel => LastLevel;
    }
}
