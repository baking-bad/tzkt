using System;
using System.Runtime.Serialization;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(OperationErrorJsonInheritanceConverter), "type")]
    [KnownType(typeof(BaseOperationError))]
    [KnownType(typeof(BalanceTooLowError))]
    [KnownType(typeof(NonExistingContractError))]
    [KnownType(typeof(UnregisteredDelegateError))]
    [KnownType(typeof(ExpressionAlreadyRegisteredError))]
    public abstract class OperationError
    {
        /// <summary>
        /// Type of an error (`error.id`, `contract.balance_too_low`, `contract.non_existing_contract`,
        /// `contract.manager.unregistered_delegate`, etc.)
        /// https://tezos.gitlab.io/api/errors.html - full list of errors
        /// </summary>
        public abstract string Type { get; set; }
    }

    public class OperationErrorJsonInheritanceConverter : JsonInheritanceConverter
    {
        public OperationErrorJsonInheritanceConverter(string name) : base(name) { }

        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(BaseOperationError))
                return "error.id";

            if (type == typeof(BalanceTooLowError))
                return "contract.balance_too_low";

            if (type == typeof(NonExistingContractError))
                return "contract.non_existing_contract";

            if (type == typeof(UnregisteredDelegateError))
                return "contract.manager.unregistered_delegate";

            if (type == typeof(ExpressionAlreadyRegisteredError))
                return "Expression_already_registered";

            return base.GetDiscriminatorValue(type);
        }
    }
}
