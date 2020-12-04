using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class VoterSnapshot
    {
        /// <summary>
        /// Voter identity
        /// </summary>
        public Alias Delegate { get; set; }

        /// <summary>
        /// Voter's rolls snapshot (aka voting power)
        /// </summary>
        public int Rolls { get; set; }
    }
}
