using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Services
{
    /// <summary>
    /// Writes numbers as strings and trims JSON up to MaxDepth
    /// </summary>
    class TokenMetadataConverter : JsonConverter<JsonElement>
    {
        readonly int MaxDepth;

        public TokenMetadataConverter(int maxDepth = 999)
        {
            MaxDepth = maxDepth;
        }

        public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Number:
                    writer.WriteStringValue(value.GetRawText());
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    if (writer.CurrentDepth <= MaxDepth)
                    {
                        foreach (var item in value.EnumerateArray())
                        {
                            Write(writer, item, options);
                        }
                    }
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    if (writer.CurrentDepth <= MaxDepth)
                    {
                        foreach (var prop in value.EnumerateObject())
                        {
                            writer.WritePropertyName(prop.Name);
                            Write(writer, prop.Value, options);
                        }
                    }
                    writer.WriteEndObject();
                    break;
                default:
                    try
                    {
                        value.WriteTo(writer);
                    }
                    catch
                    {
                        writer.WriteStringValue("Bad formatted value");
                    }
                    break;
            }
        }
    }
}
