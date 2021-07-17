using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class BakingRightTypes
    {
        public const string Baking = "baking";
        public const string Endorsing = "endorsing";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Baking => (int)BakingRightType.Baking,
                Endorsing => (int)BakingRightType.Endorsing,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)BakingRightType.Baking => Baking,
            (int)BakingRightType.Endorsing => Endorsing,
            _ => throw new Exception("invalid baking right type value")
        };
    }
}
