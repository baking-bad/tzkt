﻿using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public class JsonInt64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (!long.TryParse(reader.GetString(), out var res))
                    throw new JsonException("Failed to parse json number");

                return res;
            }

            return reader.GetInt64();
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
