using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(OperationStatusBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "applied,failed,backtracked,skipped")]
    public class OperationStatusParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=applied` is the same as `param=applied`). \
        /// Specify an operation status to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=failed`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an operation status to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=applied`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }
    }
}
