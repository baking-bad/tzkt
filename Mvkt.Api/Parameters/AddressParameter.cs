using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Mvkt.Api
{
    [ModelBinder(BinderType = typeof(AddressBinder))]
    [JsonSchemaExtensionData("x-mvkt-extension", "query-parameter")]
    public class AddressParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=` is the same as `param=`). \
        /// Specify an account address to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?address=mv123..`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an account address to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?address.ne=mv123..`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of account addresses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?address.in=mv123..,mv345..`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of account addresses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?address.ni=mv123..,mv345..`.
        /// </summary>
        public List<string> Ni { get; set; }

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

            return sb.ToString();
        }
    }
}
