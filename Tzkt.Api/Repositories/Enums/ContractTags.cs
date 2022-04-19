using System.Collections.Generic;

namespace Tzkt.Api
{
    static class ContractTags
    {
        public const string Fa1 = "fa1";
        public const string Fa12 = "fa12";
        public const string Fa2 = "fa2";

        public static bool IsValid(string value) => value switch
        {
            Fa1 => true,
            Fa12 => true,
            Fa2 => true,
            _ => false
        };

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Fa1 => (int)Data.Models.ContractTags.FA1,
                Fa12 => (int)Data.Models.ContractTags.FA12,
                Fa2 => (int)Data.Models.ContractTags.FA2,
                _ => -1
            };
            return res != -1;
        }

        public static List<string> ToList(Data.Models.ContractTags tags)
        {
            if ((tags & Data.Models.ContractTags.FA) != 0)
            {
                if ((tags & Data.Models.ContractTags.FA2) == Data.Models.ContractTags.FA2)
                    return new(1) { Fa2 };
                else if ((tags & Data.Models.ContractTags.FA12) == Data.Models.ContractTags.FA12)
                    return new(1) { Fa12 };
                else if ((tags & Data.Models.ContractTags.FA1) == Data.Models.ContractTags.FA1)
                    return new(1) { Fa1 };
            }
            return null;
        }
    }
}
