using System;
using Tzkt.Data.Models.Base;

namespace Tzkt.Api
{
    static class OpStatuses
    {
        public const string Applied = "applied";
        public const string Backtracked = "backtracked";
        public const string Skipped = "skipped";
        public const string Failed = "failed";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Applied => (int)OperationStatus.Applied,
                Backtracked => (int)OperationStatus.Backtracked,
                Skipped => (int)OperationStatus.Skipped,
                Failed => (int)OperationStatus.Failed,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)OperationStatus.Applied => Applied,
            (int)OperationStatus.Backtracked => Backtracked,
            (int)OperationStatus.Skipped => Skipped,
            (int)OperationStatus.Failed => Failed,
            _ => throw new Exception("invalid operation status value")
        };
    }
}
