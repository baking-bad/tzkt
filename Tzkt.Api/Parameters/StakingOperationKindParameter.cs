using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(StakingOperationKindBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "stake,unstake,finalize,set_parameters")]
    public class StakingOperationKindParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a staking operation kind to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?kind=stake`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a staking operation kind to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?kind.ne=finalize`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of staking operation kinds to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?kind.in=stake,unstake`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of staking operation kinds to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?kind.ni=finalize,set_parameters`.
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
