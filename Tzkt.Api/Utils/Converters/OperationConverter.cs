﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Tzkt.Api.Models;

namespace Tzkt.Api
{
    class OperationConverter : JsonConverter<Operation>
    {
        public override Operation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Operation value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
