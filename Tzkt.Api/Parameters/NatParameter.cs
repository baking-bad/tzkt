using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(NatBinder))]
    public class NatParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a `nat` value to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?balance=1234`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a `nat` value to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?balance.ne=1234`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify a `nat` value to get items where the specified field is greater than the specified value.
        /// 
        /// Example: `?balance.gt=1234`.
        /// </summary>
        public string Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify a `nat` value to get items where the specified field is greater than equal to the specified value.
        /// 
        /// Example: `?balance.ge=1234`.
        /// </summary>
        public string Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify a `nat` value to get items where the specified field is less than the specified value.
        /// 
        /// Example: `?balance.lt=1234`.
        /// </summary>
        public string Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify a `nat` value to get items where the specified field is less than or equal to the specified value.
        /// 
        /// Example: `?balance.le=1234`.
        /// </summary>
        public string Le { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of `nat` values to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?level.in=12,14,52,69`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of `nat` values to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?level.ni=12,14,52,69`.
        /// </summary>
        public List<string> Ni { get; set; }
    }
}
