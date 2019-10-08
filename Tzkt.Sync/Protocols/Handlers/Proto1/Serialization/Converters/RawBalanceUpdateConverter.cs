using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto1.Serialization
{
    class RawBalanceUpdateConverter : JsonConverter<IBalanceUpdate>
    {
        public override IBalanceUpdate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var sideReader = reader;

            sideReader.Read();
            sideReader.Read();

            if (sideReader.ValueTextEquals("freezer"))
            {
                sideReader.Read();
                sideReader.Read();
                var category = sideReader.GetString();

                return category switch
                {
                    "deposits" => JsonSerializer.Deserialize<DepositsUpdate>(ref reader, options),
                    "rewards" => JsonSerializer.Deserialize<RewardsUpdate>(ref reader, options),
                    "fees" => JsonSerializer.Deserialize<FeesUpdate>(ref reader, options),
                    _ => throw new JsonException()
                };
            }

            return JsonSerializer.Deserialize<ContractUpdate>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IBalanceUpdate value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
