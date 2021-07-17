using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class BakingRightStatuses
    {
        public const string Future = "future";
        public const string Realized = "realized";
        public const string Uncovered = "uncovered";
        public const string Missed = "missed";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Future => (int)BakingRightStatus.Future,
                Realized => (int)BakingRightStatus.Realized,
                Uncovered => (int)BakingRightStatus.Uncovered,
                Missed => (int)BakingRightStatus.Missed,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)BakingRightStatus.Future => Future,
            (int)BakingRightStatus.Realized => Realized,
            (int)BakingRightStatus.Uncovered => Uncovered,
            (int)BakingRightStatus.Missed => Missed,
            _ => throw new Exception("invalid baking right status value")
        };
    }
}
