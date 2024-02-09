﻿using Mvkt.Api.Services;

namespace Mvkt.Api
{
    public class SrPublishOperationFilter : SrOperationFilter
    {
        /// <summary>
        /// Filter by commitment
        /// </summary>
        public SrCommitmentInfoFilter commitment { get; set; }

        public override bool Empty => base.Empty && commitment == null;

        public override string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("hash", hash), ("counter", counter), ("level", level),
                ("timestamp", timestamp), ("status", status), ("sender", sender),
                ("rollup", rollup), ("commitment", commitment));
        }
    }
}
