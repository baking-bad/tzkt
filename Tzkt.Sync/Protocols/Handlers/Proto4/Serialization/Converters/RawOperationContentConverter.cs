using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tzkt.Sync.Protocols.Proto4.Serialization
{
    class RawOperationContentConverter : JsonConverter<IOperationContent>
    {
        public override IOperationContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var sideReader = reader;

            sideReader.Read();
            sideReader.Read();
            var kind = sideReader.GetString();

            return kind switch
            {
                "endorsement" => JsonSerializer.Deserialize<RawEndorsementContent>(ref reader, options),
                "reveal" => JsonSerializer.Deserialize<RawRevealContent>(ref reader, options),
                "transaction" => JsonSerializer.Deserialize<RawTransactionContent>(ref reader, options),
                "activate_account" => JsonSerializer.Deserialize<RawActivationContent>(ref reader, options),
                "delegation" => JsonSerializer.Deserialize<RawDelegationContent>(ref reader, options),
                "origination" => JsonSerializer.Deserialize<RawOriginationContent>(ref reader, options),
                "seed_nonce_revelation" => JsonSerializer.Deserialize<RawNonceRevelationContent>(ref reader, options),
                "double_baking_evidence" => JsonSerializer.Deserialize<RawDoubleBakingEvidenceContent>(ref reader, options),
                "double_endorsement_evidence" => JsonSerializer.Deserialize<RawDoubleEndorsingEvidenceContent>(ref reader, options),
                "proposals" => JsonSerializer.Deserialize<RawProposalContent>(ref reader, options),
                "ballot" => JsonSerializer.Deserialize<RawBallotContent>(ref reader, options),
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, IOperationContent value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
