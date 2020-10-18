using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(MigrationKindBinder))]
    public class MigrationKindParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a migration kind to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?kind=bootstrap`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a migration kind to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=proposal_invoice`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of migration kinds to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=bootstrap,proposal_invoice`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of migration kinds to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=airdrop,bootstrap`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }
    }
}
