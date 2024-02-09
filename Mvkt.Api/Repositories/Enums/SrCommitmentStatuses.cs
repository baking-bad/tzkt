using Mvkt.Data.Models;

namespace Mvkt.Api
{
    static class SrCommitmentStatuses
    {
        public const string Orphan = "orphan";
        public const string Refuted = "refuted";
        public const string Pending = "pending";
        public const string Cemented = "cemented";
        public const string Executed = "executed";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Orphan => (int)SmartRollupCommitmentStatus.Orphan,
                Refuted => (int)SmartRollupCommitmentStatus.Refuted,
                Pending => (int)SmartRollupCommitmentStatus.Pending,
                Cemented => (int)SmartRollupCommitmentStatus.Cemented,
                Executed => (int)SmartRollupCommitmentStatus.Executed,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)SmartRollupCommitmentStatus.Orphan => Orphan,
            (int)SmartRollupCommitmentStatus.Refuted => Refuted,
            (int)SmartRollupCommitmentStatus.Pending => Pending,
            (int)SmartRollupCommitmentStatus.Cemented => Cemented,
            (int)SmartRollupCommitmentStatus.Executed => Executed,
            _ => throw new Exception("invalid smart rollup commitment status value")
        };
    }
}
