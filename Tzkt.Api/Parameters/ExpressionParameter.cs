using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(ExpressionBinder))]
    public class ExpressionParameter
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
    }
}
