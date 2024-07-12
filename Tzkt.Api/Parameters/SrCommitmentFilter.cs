using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrCommitmentFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by initiator (an account published the commitment first).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter initiator { get; set; }

        /// <summary>
        /// Filter by smart rollup.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter rollup { get; set; }

        /// <summary>
        /// Filter by inbox level.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter inboxLevel { get; set; }

        /// <summary>
        /// Filter by commitment hash.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Src1HashParameter hash { get; set; }

        /// <summary>
        /// Filter by level of the block, where the commitment was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the commitment was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block, where the commitment was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the commitment was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        /// <summary>
        /// Filter by commitment status (`pending`, `cemented`, `executed`, `refuted`, or `orphan`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SrCommitmentStatusParameter status { get; set; }

        /// <summary>
        /// Filter by predecessor commitment.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public SrCommitmentInfoFilter predecessor { get; set; }

        [OpenApiIgnore]
        public bool Empty => 
            id == null &&
            initiator == null &&
            rollup == null &&
            inboxLevel == null &&
            hash == null &&
            firstLevel == null &&
            firstTime == null &&
            lastLevel == null &&
            lastTime == null &&
            status == null &&
            (predecessor == null || predecessor.Empty);

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("initiator", initiator), ("rollup", rollup), ("inboxLevel", inboxLevel),
                ("hash", hash), ("firstLevel", firstLevel), ("firstTime", firstTime), ("lastLevel", lastLevel),
                ("lastTime", lastTime), ("status", status), ("predecessor", predecessor));
        }
    }
}
