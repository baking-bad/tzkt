using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    class RawJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            using var json = JsonDocument.Parse(value);
            json.WriteTo(writer);
        }
    }
}
