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
            if (value.Rows.Length == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
            else if (value.Rows[0].Length == 1)
            {
                writer.WriteStartArray();
                for (int i = 0; i < value.Rows.Length; i++)
                {
                    if (value.Rows[i][0] == null)
                        writer.WriteNullValue();
                    else
                        JsonSerializer.Serialize(writer, value.Rows[i][0], options);
                }
                writer.WriteEndArray();
            }
            else if (value.Cols == null)
            {
                var cols = value.Rows[0].Length;
                writer.WriteStartArray();
                for (int i = 0; i < value.Rows.Length; i++)
                {
                    writer.WriteStartArray();
                    for (int j = 0; j < cols; j++)
                    {
                        if (value.Rows[i][j] == null)
                            writer.WriteNullValue();
                        else
                            JsonSerializer.Serialize(writer, value.Rows[i][j], options);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }
            else
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
}
