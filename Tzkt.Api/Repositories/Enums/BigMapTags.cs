using System.Collections.Generic;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class BigMapTags
    {
        public const string TokenMetadata = "token_metadata";
        public const string Metadata = "metadata";

        public static bool IsValid(string value) => value switch
        {
            TokenMetadata => true,
            Metadata => true,
            _ => false
        };

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                TokenMetadata => (int)BigMapTag.TokenMetadata,
                Metadata => (int)BigMapTag.Metadata,
                _ => -1
            };
            return res != -1;
        }

        public static List<string> ToList(BigMapTag tags) => tags switch
        {
            BigMapTag.TokenMetadata => new(1) { TokenMetadata },
            BigMapTag.Metadata => new(1) { Metadata },
            _ => null
        };
    }
}
