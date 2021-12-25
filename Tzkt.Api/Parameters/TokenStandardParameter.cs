using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(TokenStandardBinder))]
    public class TokenStandardParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify token standard (`fa1.2` or `fa2`) to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=fa2`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify token standard (`fa1.2` or `fa2`) to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=fa1.2`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }
    }
}
