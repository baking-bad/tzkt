using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrGameFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by smart rollup.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter rollup { get; set; }

        /// <summary>
        /// Filter by initiator (who found a wrong commitment and started the refutation game).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter initiator { get; set; }

        /// <summary>
        /// Filter by initiator's commitment
        /// </summary>
        public SrCommitmentInfoFilter initiatorCommitment { get; set; }

        /// <summary>
        /// Filter by opponent (who was acused in publishing a wrong commitment).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter opponent { get; set; }

        /// <summary>
        /// Filter by opponent's commitment
        /// </summary>
        public SrCommitmentInfoFilter opponentCommitment { get; set; }

        /// <summary>
        /// Filter by level of the block, where the refutation game was started.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the refutation game was started.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block, where the refutation game was last updated.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp of the block, where the refutation game was last updated.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        [OpenApiIgnore]
        public bool Empty => 
            id == null &&
            rollup == null &&
            initiator == null &&
            initiatorCommitment.Empty &&
            opponent == null &&
            opponentCommitment.Empty &&
            firstLevel == null &&
            firstTime == null &&
            lastLevel == null &&
            lastTime == null;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("rollup", rollup), ("initiator", initiator), ("initiatorCommitment", initiatorCommitment),
                ("opponent", opponent), ("opponentCommitment", opponentCommitment), ("firstLevel", firstLevel),
                ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime));
        }
    }
}
