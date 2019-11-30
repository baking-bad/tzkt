using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(OperationErrorJsonInheritanceConverter), "type")]
    [KnownType(typeof(BaseOperationError))]
    [KnownType(typeof(BalanceTooLowError))]
    [KnownType(typeof(NonExistingContractError))]
    [KnownType(typeof(UnregisteredDelegateError))]
    public abstract class OperationError
    {
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

            return base.GetDiscriminatorValue(type);
        }
    }
}
