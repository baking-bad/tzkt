using System;
using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class ContractKinds
    {
        public const string Delegator = "delegator_contract";
        public const string SmartContract = "smart_contract";
        public const string Asset = "asset";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Delegator => (int)ContractKind.DelegatorContract,
                SmartContract => (int)ContractKind.SmartContract,
                Asset => (int)ContractKind.Asset,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)ContractKind.DelegatorContract => Delegator,
            (int)ContractKind.SmartContract => SmartContract,
            (int)ContractKind.Asset => Asset,
            _ => throw new Exception("invalid contract kind value")
        };
    }
}
