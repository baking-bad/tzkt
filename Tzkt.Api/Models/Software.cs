using System;
using System.Collections.Generic;

namespace Tzkt.Api.Models
{
    public class Software
    {
        /// <summary>
        /// Software ID (short commit hash)
        /// </summary>
        public string ShortHash { get; set; }

        /// <summary>
        /// Level of the first block baked with this software
        /// </summary>
        public int FirstLevel { get; set; }

        /// <summary>
        /// Datetime of the first block baked with this software
        /// </summary>
        public DateTime FirstTime { get; set; }

        /// <summary>
        /// Level of the last block baked with this software
        /// </summary>
        public int LastLevel { get; set; }

        /// <summary>
        /// Datetime of the last block baked with this software
        /// </summary>
        public DateTime LastTime { get; set; }

        /// <summary>
        /// Total number of blocks baked with this software
        /// </summary>
        public int BlocksCount { get; set; }

        /// <summary>
        /// Offchain data: commit date
        /// </summary>
        public DateTime? CommitDate { get; set; }

        /// <summary>
        /// Offchain data: commit hash
        /// </summary>
        public string CommitHash { get; set; }

        /// <summary>
        /// Offchain data: software version (commit tag)
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Offchain data: software tags, e.g. `docker`, `staging` etc.
        /// </summary>
        public List<string> Tags { get; set; }
    }
}
