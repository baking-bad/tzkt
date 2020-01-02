using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync.Protocols.Proto5.Serialization
{
    class NSRawBalanceUpdateConverter : JsonConverter<IBalanceUpdate>
    {
        public override IBalanceUpdate ReadJson(JsonReader reader, Type objectType, IBalanceUpdate existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JObject.Load(reader);

            if (token["kind"].Value<string>() == "freezer")
            {
                return token["category"].Value<string>() switch
                {
                    "deposits" => token.ToObject<DepositsUpdate>(),
                    "rewards" => token.ToObject<RewardsUpdate>(),
                    "fees" => token.ToObject<FeesUpdate>(),
                    _ => throw new JsonException()
                };
            }

            return token.ToObject<ContractUpdate>();
        }

        public override void WriteJson(JsonWriter writer, IBalanceUpdate value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
