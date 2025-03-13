using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(UnstakeRequestStatusBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "pending,finalizable,finalized")]
    public class UnstakeRequestStatusParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (`.eq` suffix can be omitted, i.e. `?param=...` is the same as `?param.eq=...`). \
        /// Specify an unstake request status to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?status=pending`.
        /// </summary>
        public string? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an unstake request status to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?status.ne=finalized`.
        /// </summary>
        public string? Ne { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={Ne}&");
            }

            return sb.ToString();
        }
    }
}
