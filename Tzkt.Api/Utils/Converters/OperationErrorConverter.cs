using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tzkt.Api.Models;

namespace Tzkt.Api
{
    class OperationErrorConverter : JsonConverter<OperationError>
    {
        public override OperationError Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var sideReader = reader;

            sideReader.Read();
            while (!sideReader.ValueTextEquals("type"))
            {
                sideReader.Skip();
                sideReader.Read();
            }

            sideReader.Read();
            var type = sideReader.GetString();

            return type switch
            {
                "contract.balance_too_low" => JsonSerializer.Deserialize<BalanceTooLowError>(ref reader, options),
                "contract.manager.unregistered_delegate" => JsonSerializer.Deserialize<UnregisteredDelegateError>(ref reader, options),
                "contract.non_existing_contract" => JsonSerializer.Deserialize<NonExistingContractError>(ref reader, options),
                "Expression_already_registered" => JsonSerializer.Deserialize<ExpressionAlreadyRegisteredError>(ref reader, options),
                _ => JsonSerializer.Deserialize<BaseOperationError>(ref reader, options)
            };
        }

        public override void Write(Utf8JsonWriter writer, OperationError value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    static class OperationErrorSerializer
    {
        public static JsonSerializerOptions Options { get; }

        static OperationErrorSerializer()
        {
            Options = new JsonSerializerOptions();
            Options.Converters.Add(new OperationErrorConverter());
        }

        public static List<OperationError> Deserialize(string json)
            => JsonSerializer.Deserialize<List<OperationError>>(json, Options);
    }
}
