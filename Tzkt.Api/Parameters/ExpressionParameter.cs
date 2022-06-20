using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(ExpressionBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class ExpressionParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify an expression hash to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?address=expr...`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an expression hash to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?address.ne=expr...`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of expression hashes to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?address.in=expr1,expr2`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of expression hashes to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?address.ni=expr1,expr2`.
        /// </summary>
        public List<string> Ni { get; set; }

        #region operators
        public static implicit operator ExpressionParameter(string value) => new() { Eq = value };
        #endregion

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
