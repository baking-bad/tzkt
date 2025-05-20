using System.Numerics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(TokenGlobalIdBinder))]
    public class TokenGlobalIdParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify token standard (`fa1.2` or `fa2`) to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?type=fa2`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public (int, BigInteger)? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify token standard (`fa1.2` or `fa2`) to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=fa1.2`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(int, BigInteger)>? In { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq.Value.Item1}:{Eq.Value.Item2}&");
            }


            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.Select(x => $"{x.Item1}:{x.Item2}").OrderBy(x => x))}&");
            }

            return sb.ToString();
        }
    }
}
