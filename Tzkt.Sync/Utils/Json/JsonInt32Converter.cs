using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public class JsonInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (!int.TryParse(reader.GetString(), out var res))
                    throw new JsonException("Failed to parse json number");

                return res;
            }

            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
