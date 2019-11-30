using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(OperationJsonInheritanceConverter), "type")]
    [KnownType(typeof(EndorsementOperation))]
    [KnownType(typeof(BallotOperation))]
    [KnownType(typeof(ProposalOperation))]
    [KnownType(typeof(ActivationOperation))]
    [KnownType(typeof(DoubleBakingOperation))]
    [KnownType(typeof(DoubleEndorsingOperation))]
    [KnownType(typeof(NonceRevelationOperation))]
    [KnownType(typeof(DelegationOperation))]
    [KnownType(typeof(OriginationOperation))]
    [KnownType(typeof(TransactionOperation))]
    [KnownType(typeof(RevealOperation))]
    [KnownType(typeof(SystemOperation))]
    public abstract class Operation
    {
        public abstract string Type { get; }

        public abstract int Id { get; set; }
    }

    public class OperationJsonInheritanceConverter : JsonInheritanceConverter
    {
        public OperationJsonInheritanceConverter(string name) : base(name) { }

        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(EndorsementOperation))
                return OpTypes.Endorsement;

            if (type == typeof(BallotOperation))
                return OpTypes.Ballot;

            if (type == typeof(ProposalOperation))
                return OpTypes.Proposal;

            if (type == typeof(ActivationOperation))
                return OpTypes.Activation;

            if (type == typeof(DoubleBakingOperation))
                return OpTypes.DoubleBaking;

            if (type == typeof(DoubleEndorsingOperation))
                return OpTypes.DoubleEndorsing;

            if (type == typeof(NonceRevelationOperation))
                return OpTypes.NonceRevelation;

            if (type == typeof(DelegationOperation))
                return OpTypes.Delegation;

            if (type == typeof(OriginationOperation))
                return OpTypes.Origination;

            if (type == typeof(TransactionOperation))
                return OpTypes.Transaction;

            if (type == typeof(RevealOperation))
                return OpTypes.Reveal;

            if (type == typeof(SystemOperation))
                return OpTypes.System;

            return base.GetDiscriminatorValue(type);
        }
    }
}
