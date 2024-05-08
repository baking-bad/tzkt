using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    class BigIntegerNullableConverter : JsonConverter<BigInteger?>
    {
        public override BigInteger? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && BigInteger.TryParse(reader.GetString(), out var res))
                return res;

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var int64))
                return new(int64);

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            throw new JsonException("Failed to parse BigInteger? value");
        }

        public override void Write(Utf8JsonWriter writer, BigInteger? value, JsonSerializerOptions options)
        {
            if (value is BigInteger bigint)
                writer.WriteStringValue(bigint.ToString());
            else
                writer.WriteNullValue();
        }
    }
}
