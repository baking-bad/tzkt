using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(RefutationMoveBinder))]
    [JsonSchemaExtensionData("x-mvkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-mvkt-query-parameter", "start,dissection,proof,timeout")]
    public class RefutationMoveParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (`.eq` suffix can be omitted, i.e. `?param=...` is the same as `?param.eq=...`). \
        /// Specify a refutation game move to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?move=start`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a refutation game move to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?move.ne=start`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of refutation game moves to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?move.in=proof,timeout`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of refutation game moves to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?move.ni=proof,timeout`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }

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
