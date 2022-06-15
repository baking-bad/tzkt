using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(Int32Binder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class Int32Parameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify an integer number to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?balance=1234`.
        /// </summary>
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an integer number to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?balance.ne=1234`.
        /// </summary>
        public int? Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify an integer number to get items where the specified field is greater than the specified value.
        /// 
        /// Example: `?balance.gt=1234`.
        /// </summary>
        public int? Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify an integer number to get items where the specified field is greater than equal to the specified value.
        /// 
        /// Example: `?balance.ge=1234`.
        /// </summary>
        public int? Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify an integer number to get items where the specified field is less than the specified value.
        /// 
        /// Example: `?balance.lt=1234`.
        /// </summary>
        public int? Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify an integer number to get items where the specified field is less than or equal to the specified value.
        /// 
        /// Example: `?balance.le=1234`.
        /// </summary>
        public int? Le { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of integers to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?level.in=12,14,52,69`.
        /// </summary>
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of integers to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?level.ni=12,14,52,69`.
        /// </summary>
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

            if (Gt != null)
            {
                sb.Append($"{name}.gt={Gt}&");
            }

            if (Ge != null)
            {
                sb.Append($"{name}.ge={Ge}&");
            }

            if (Lt != null)
            {
                sb.Append($"{name}.lt={Lt}&");
            }

            if (Le != null)
            {
                sb.Append($"{name}.le={Le}&");
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
