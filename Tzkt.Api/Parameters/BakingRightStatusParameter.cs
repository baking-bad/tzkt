using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BakingRightStatusBinder))]
    public class BakingRightStatusParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify baking right status to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=future`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify baking right status to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=missed`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }
    }
}
