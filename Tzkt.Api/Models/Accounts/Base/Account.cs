using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.Converters;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(AccountJsonInheritanceConverter), "type")]
    [KnownType(typeof(User))]
    [KnownType(typeof(Delegate))]
    [KnownType(typeof(Contract))]
    [KnownType(typeof(Ghost))]
    [KnownType(typeof(Rollup))]
    [KnownType(typeof(EmptyAccount))]
    public abstract class Account
    {
        /// <summary>
        /// Type of the account (`user` - simple wallet account, `delegate` - account, registered as a delegate (baker),
        /// `contract` - smart contract programmable account, `ghost` - non-existent contract, `empty` - account hasn't appeared in the blockchain yet,
        /// `rollup` - transactional optimistic rollup)
        /// </summary>
        public abstract string Type { get; }
        
        public abstract string Address { get; set; }
    }

    public class AccountJsonInheritanceConverter : JsonInheritanceConverter
    {
        public AccountJsonInheritanceConverter(string name) : base(name) { }
        
        public override string GetDiscriminatorValue(Type type)
        {
            if (type == typeof(Delegate))
                return AccountTypes.Delegate;

            if (type == typeof(User))
                return AccountTypes.User;

            if (type == typeof(Contract))
                return AccountTypes.Contract;

            if (type == typeof(Ghost))
                return AccountTypes.Ghost;

            if (type == typeof(Rollup))
                return AccountTypes.Rollup;

            if (type == typeof(EmptyAccount))
                return AccountTypes.Empty;

            return base.GetDiscriminatorValue(type);
        }
    }
}
