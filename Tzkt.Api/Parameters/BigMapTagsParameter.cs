using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BigMapTagsBinder))]
    public class BigMapTagsParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a contract kind to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?kind=smart_contract`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Has any** filter mode. \
        /// Specify a comma-separated list of contract kinds to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?kind.in=smart_contract,asset`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Any { get; set; }

        /// <summary>
        /// **Has all** filter mode. \
        /// Specify a comma-separated list of contract kinds to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?kind.ni=smart_contract,asset`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? All { get; set; }
    }
}
