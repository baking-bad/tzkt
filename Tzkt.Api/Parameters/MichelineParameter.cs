using System.Text;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(MichelineBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class MichelineParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify Micheline JSON value to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type={"prim":"string"}`.
        /// </summary>
        public IMicheline Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify Micheline JSON value to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?balance.ne={"prim":"string"}`.
        /// </summary>
        public IMicheline Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of Micheline JSON values where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?type.in={"prim":"string"},{"prim":"nat"}`.
        /// </summary>
        public List<IMicheline> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of Micheline JSON values where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?type.ni={"prim":"string"},{"prim":"nat"}`.
        /// </summary>
        public List<IMicheline> Ni { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Hex.Convert(Eq.ToBytes())}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={Hex.Convert(Ne.ToBytes())}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.Select(x => Hex.Convert(x.ToBytes())).OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.Select(x => Hex.Convert(x.ToBytes())).OrderBy(x => x))}&");
            }

            return sb.ToString();
        }
    }
}
