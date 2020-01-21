using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync.Protocols.Proto6.Serialization
{
    class NSRawOperationContentConverter : JsonConverter<IOperationContent>
    {
        static readonly JsonSerializer JS;
        static NSRawOperationContentConverter()
        {
            JS = new JsonSerializer();
            JS.Converters.Add(new NSJsonElementConverter());
            JS.Converters.Add(new NSRawBalanceUpdateConverter());
            JS.Converters.Add(new NSRawInternalOperationResultConverter());
        }

        public override IOperationContent ReadJson(JsonReader reader, Type objectType, IOperationContent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JObject.Load(reader);

            return token["kind"].Value<string>() switch
            {
                "endorsement" => token.ToObject<RawEndorsementContent>(JS),
                "reveal" => token.ToObject<RawRevealContent>(JS),
                "transaction" => token.ToObject<RawTransactionContent>(JS),
                "activate_account" => token.ToObject<RawActivationContent>(JS),
                "delegation" => token.ToObject<RawDelegationContent>(JS),
                "origination" => token.ToObject<RawOriginationContent>(JS),
                "seed_nonce_revelation" => token.ToObject<RawNonceRevelationContent>(JS),
                "double_baking_evidence" => token.ToObject<RawDoubleBakingEvidenceContent>(JS),
                "double_endorsement_evidence" => token.ToObject<RawDoubleEndorsingEvidenceContent>(JS),
                "proposals" => token.ToObject<RawProposalContent>(JS),
                "ballot" => token.ToObject<RawBallotContent>(JS),
                _ => throw new JsonException()
            };
        }

        public override void WriteJson(JsonWriter writer, IOperationContent value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
