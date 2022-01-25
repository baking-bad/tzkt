using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AccountBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class AccountParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a `tz` or `KT` address to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?sender=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a `tz` or `KT` address to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?sender.ne=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of addresses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=tz1WnfXMPaNTB,tz1SiPXX4MYGNJND`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of addresses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=tz1WnfXMPaNTB,tz1SiPXX4MYGNJND`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }

        /// <summary>
        /// **Equal to another field** filter mode. \
        /// Specify a field name to get items where the specified fields are equal.
        /// 
        /// Example: `?sender.eqx=target`.
        /// </summary>
        public string Eqx { get; set; }

        /// <summary>
        /// **Not equal to another field** filter mode. \
        /// Specify a field name to get items where the specified fields are not equal.
        /// 
        /// Example: `?sender.nex=initiator`.
        /// </summary>
        public string Nex { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?initiator.null` or `?initiator.null=false`.
        /// </summary>
        public bool? Null { get; set; }
    }
}
