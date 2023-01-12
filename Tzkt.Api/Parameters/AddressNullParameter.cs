using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AddressNullBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class AddressNullParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=` is the same as `param=`). \
        /// Specify an account address to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?address=tz123..`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an account address to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?address.ne=tz123..`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of account addresses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?address.in=tz123..,tz345..`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of account addresses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?address.ni=tz123..,tz345..`.
        /// </summary>
        public List<string> Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?address.null` or `?address.null=false`.
        /// </summary>
        public bool? Null { get; set; }

        [JsonIgnore]
        public bool InHasNull { get; set; }

        [JsonIgnore]
        public bool NiHasNull { get; set; }

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

            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.OrderBy(x => x))}&");
            }

            if (Null != null)
            {
                sb.Append($"{name}.null={Null}&");
            }

            sb.Append($"{name}.NiHasNull={NiHasNull}&");
            sb.Append($"{name}.InHasNull={InHasNull}&");

            return sb.ToString();
        }
    }
}
