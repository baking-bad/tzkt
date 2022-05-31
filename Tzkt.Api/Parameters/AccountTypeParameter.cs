using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AccountTypeBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "user,contract,delegate")]
    public class AccountTypeParameter : INormalized
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify an account type to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=delegate`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an account type to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=contract`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        public string Normalize(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
