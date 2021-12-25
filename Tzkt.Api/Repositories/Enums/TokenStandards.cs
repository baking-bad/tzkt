using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class TokenStandards
    {
        public const string Fa12 = "fa1.2";
        public const string Fa2 = "fa2";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Fa12 => (int)TokenTags.Fa12,
                Fa2 => (int)TokenTags.Fa2,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int tags) => ((TokenTags)tags).HasFlag(TokenTags.Fa2) ? Fa2 : Fa12;
    }
}
