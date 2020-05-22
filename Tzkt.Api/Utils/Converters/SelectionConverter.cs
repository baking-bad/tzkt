using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    class SelectionConverter : JsonConverter<SelectionResponse>
    {
        public override SelectionResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SelectionResponse value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            for (int i = 0; i < value.Rows.Length; i++)
            {
                writer.WriteStartObject();
                for (int j = 0; j < value.Cols.Length; j++)
                {
                    if (value.Rows[i][j] == null)
                        writer.WriteNull(value.Cols[j]);
                    else
                    {
                        writer.WritePropertyName(value.Cols[j]);
                        JsonSerializer.Serialize(writer, value.Rows[i][j], options);
                    }
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
