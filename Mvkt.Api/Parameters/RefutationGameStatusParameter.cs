using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Mvkt.Api
{
    [ModelBinder(BinderType = typeof(RefutationGameStatusBinder))]
    [JsonSchemaExtensionData("x-mvkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-mvkt-query-parameter", "none,ongoing,loser,draw")]
    public class RefutationGameStatusParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (`.eq` suffix can be omitted, i.e. `?param=...` is the same as `?param.eq=...`). \
        /// Specify a refutation game status to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?gameStatus=draw`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a refutation game status to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?gameStatus.ne=ongoing`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of refutation game statuses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?gameStatus.in=loser,draw`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of refutation game statuses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?gameStatus.ni=loser,draw`.
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
