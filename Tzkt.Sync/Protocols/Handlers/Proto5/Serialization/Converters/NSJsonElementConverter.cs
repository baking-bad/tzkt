using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync.Protocols.Proto5.Serialization
{
    class NSJsonElementConverter : JsonConverter<System.Text.Json.JsonElement>
    {
        public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var json = token.ToString();
            var doc = System.Text.Json.JsonDocument.Parse(json);

            return doc.RootElement;
        }

        public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
