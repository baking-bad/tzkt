using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync.Protocols
{
    public static class BalanceUpdates
    {
        public static List<IBalanceUpdate> Parse(JArray json)
        {
            var result = new List<IBalanceUpdate>(json.Count);
            foreach (var item in json)
            {
                result.Add((item["kind"]?.String(), item["category"]?.String()) switch
                {
                    ("contract", null) => item.ToObject<ContractUpdate>(),
                    ("freezer", "deposits") => item.ToObject<DepositsUpdate>(),
                    ("freezer", "rewards") => item.ToObject<RewardsUpdate>(),
                    ("freezer", "fees") => item.ToObject<FeesUpdate>(),
                    (var c, var k) => throw new Exception($"Invalid balance update item ('{c}', '{k}')")
                });
            }
            return result;
        }
    }

    public interface IBalanceUpdate
    {
        public long Change { get; }
    }

    public class ContractUpdate : IBalanceUpdate
    {
        [JsonProperty(Required = Required.Always)]
        public string Contract { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public long Change { get; set; }
    }

    public class FreezerUpdate : IBalanceUpdate
    {
        [JsonProperty(Required = Required.Always)]
        public string Delegate { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public int Level { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public long Change { get; set; }
    }

    public class DepositsUpdate : FreezerUpdate { }
    public class RewardsUpdate : FreezerUpdate { }
    public class FeesUpdate : FreezerUpdate { }
}
