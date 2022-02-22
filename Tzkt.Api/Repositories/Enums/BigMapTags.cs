using System.Collections.Generic;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class BigMapTags
    {
        public const string Metadata = "metadata";
        public const string TokenMetadata = "token_metadata";
        public const string Ledger = "ledger";

        public static bool IsValid(string value) => value switch
        {
            Metadata => true,
            TokenMetadata => true,
            Ledger => true,
            _ => false
        };

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Metadata => (int)BigMapTag.Metadata,
                TokenMetadata => (int)BigMapTag.TokenMetadata,
                Ledger => (int)BigMapTag.Ledger,
                _ => -1
            };
            return res != -1;
        }

        public static List<string> ToList(BigMapTag tags)
        {
            if (tags >= BigMapTag.Metadata)
            {
                if ((tags & BigMapTag.Metadata) != 0)
                    return new(1) { Metadata };
                else if ((tags & BigMapTag.TokenMetadata) != 0)
                    return new(1) { TokenMetadata };
                else if ((tags & BigMapTag.Ledger) != 0)
                    return new(1) { Ledger };
            }
            return null;
        }
    }
}
