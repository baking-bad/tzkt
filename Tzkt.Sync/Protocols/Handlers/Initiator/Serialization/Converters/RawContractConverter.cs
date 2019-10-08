using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Initiator.Serialization
{
    class RawContractConverter : JsonConverter<RawContract>
    {
        public override RawContract Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            var address = reader.GetString();

            reader.Read();
            var contract = JsonSerializer.Deserialize<RawContract>(ref reader, SerializerOptions.Default);
            contract.Address = address;

            reader.Read();
            return contract;
        }

        public override void Write(Utf8JsonWriter writer, RawContract value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
