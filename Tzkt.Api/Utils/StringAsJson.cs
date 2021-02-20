using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    [JsonConverter(typeof(StringAsJsonConverter))]
    public class StringAsJson
    {
        public string Json { get; }
        public StringAsJson(string json) => Json = json;
    }

    class StringAsJsonConverter : JsonConverter<StringAsJson>
    {
        public override StringAsJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, StringAsJson value, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.Parse(value.Json, new JsonDocumentOptions { MaxDepth = 1024 });
            doc.WriteTo(writer);
        }
    }
}
