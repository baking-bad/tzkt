using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NJsonSchema.Converters;
using Newtonsoft.Json;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(AccountJsonInheritanceConverter), "type")]
    [KnownType(typeof(User))]
    [KnownType(typeof(Delegate))]
    [KnownType(typeof(Contract))]
    [KnownType(typeof(EmptyAccount))]
    public abstract class Account
    {
        public abstract string Type { get; }
    }

    public class AccountJsonInheritanceConverter : JsonInheritanceConverter
    {
        public AccountJsonInheritanceConverter(string name) : base(name) { }
        
        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(Delegate))
                return "delegate";

            if (type == typeof(User))
                return "user";

            if (type == typeof(Contract))
                return "contract";

            if (type == typeof(EmptyAccount))
                return "empty";

            return base.GetDiscriminatorValue(type);
        }
    }
}
