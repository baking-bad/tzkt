using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class SrPublishOperationFilter : SrOperationFilter
    {
        /// <summary>
        /// Filter by commitment
        /// </summary>
        public SrCommitmentInfoFilter commitment { get; set; } = new();

        public override bool Empty => base.Empty && commitment.Empty;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("rollup", rollup), ("commitment", commitment));
        }
    }
}
