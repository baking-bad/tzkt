using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class SecondaryKeyTypes
    {
        public const string Consensus = "consensus";
        public const string Companion = "companion";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Consensus => (int)SecondaryKeyType.Consensus,
                Companion => (int)SecondaryKeyType.Companion,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)SecondaryKeyType.Consensus => Consensus,
            (int)SecondaryKeyType.Companion => Companion,
            _ => throw new Exception("invalid secondary key type value")
        };
    }
}
