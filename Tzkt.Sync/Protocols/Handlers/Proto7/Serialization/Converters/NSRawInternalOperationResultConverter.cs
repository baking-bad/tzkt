using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync.Protocols.Proto7.Serialization
{
    class NSRawInternalOperationResultConverter : JsonConverter<IInternalOperationResult>
    {
        static readonly JsonSerializer JS;
        static NSRawInternalOperationResultConverter()
        {
            JS = new JsonSerializer();
            JS.Converters.Add(new NSJsonElementConverter());
            JS.Converters.Add(new NSRawBalanceUpdateConverter());
        }

        public override IInternalOperationResult ReadJson(JsonReader reader, Type objectType, IInternalOperationResult existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JObject.Load(reader);

            return token["kind"].Value<string>() switch
            {
                "delegation" => token.ToObject<RawInternalDelegationResult>(JS),
                "origination" => token.ToObject<RawInternalOriginationResult>(JS),
                "transaction" => token.ToObject<RawInternalTransactionResult>(JS),
                _ => throw new JsonException()
            };
        }

        public override void WriteJson(JsonWriter writer, IInternalOperationResult value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
