using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(ContractKindBinder))]
    public class ContractKindParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a contract kind to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?kind=smart_contract`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a contract kind to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?kind.ne=delegator_contract`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of contract kinds to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?kind.in=smart_contract,asset`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of contract kinds to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?kind.ni=smart_contract,asset`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }
    }
}
