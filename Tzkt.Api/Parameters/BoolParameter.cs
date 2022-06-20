using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BoolBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class BoolParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=true` is the same as `param=true`). \
        /// Specify a bool flag to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?active=true` or `?active=1` or `?active`.
        /// </summary>
        public bool? Eq { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?active.null` or `?active.null=false`.
        /// </summary>
        public bool? Null { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq}&");
            }

            if (Null != null)
            {
                sb.Append($"{name}.null={Null}&");
            }
            
            return sb.ToString();
        }
    }
}
