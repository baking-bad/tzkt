using NSwag.Annotations;
using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrGameInfoFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

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

        [OpenApiIgnore]
        public bool Empty => 
            id == null &&
            initiator == null &&
            initiatorCommitment.Empty &&
            opponent == null &&
            opponentCommitment.Empty;

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("initiator", initiator), ("initiatorCommitment", initiatorCommitment),
                ("opponent", opponent), ("opponentCommitment", opponentCommitment));
        }
    }
}
