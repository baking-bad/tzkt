using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BakingRightTypeBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "baking,endorsing")]
    public class BakingRightTypeParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify baking right type to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=baking`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify baking right type to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=endorsing`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }
    }
}
