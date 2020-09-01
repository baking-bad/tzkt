using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    class SelectionSingleConverter : JsonConverter<SelectionSingleResponse>
    {
        public override SelectionSingleResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SelectionSingleResponse value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            for (int j = 0; j < value.Cols.Length; j++)
            {
                if (value.Vals[j] == null)
                    writer.WriteNull(value.Cols[j]);
                else
                {
                    writer.WritePropertyName(value.Cols[j]);
                    JsonSerializer.Serialize(writer, value.Vals[j], options);
                }
            }
            writer.WriteEndObject();
        }
    }
}
