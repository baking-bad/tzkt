using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    [JsonConverter(typeof(JsonStringConverter))]
    public class JsonString
    {
        public string Json { get; }
        public JsonString(string json) => Json = json;

        public static implicit operator JsonString (string value) => new JsonString(value);
        public static explicit operator string (JsonString value) => value.Json;
    }

    class JsonStringConverter : JsonConverter<JsonString>
    {
        public override JsonString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, JsonString value, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.Parse(value.Json, new JsonDocumentOptions { MaxDepth = 1024 });
            doc.WriteTo(writer);
        }
    }
}
