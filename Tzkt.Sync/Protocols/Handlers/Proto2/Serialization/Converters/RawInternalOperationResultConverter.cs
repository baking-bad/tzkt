using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto2.Serialization
{
    class RawInternalOperationResultConverter : JsonConverter<IInternalOperationResult>
    {
        public override IInternalOperationResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var sideReader = reader;

            sideReader.Read();
            sideReader.Read();
            var kind = sideReader.GetString();

            return kind switch
            {
                "transaction" => JsonSerializer.Deserialize<RawInternalTransactionResult>(ref reader, options),
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, IInternalOperationResult value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
