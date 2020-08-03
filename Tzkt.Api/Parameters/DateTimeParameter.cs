using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(DateTimeBinder))]
    public class DateTimeParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=2020-01-01` is the same as `param=2020-01-01`). \
        /// Specify a datetime to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?timestamp=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a datetime to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?timestamp.ne=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify a datetime to get items where the specified field is greater than the specified value.
        /// 
        /// Example: `?timestamp.gt=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify a datetime to get items where the specified field is greater than equal to the specified value.
        /// 
        /// Example: `?timestamp.ge=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify a datetime to get items where the specified field is less than the specified value.
        /// 
        /// Example: `?timestamp.lt=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify a datetime to get items where the specified field is less than or equal to the specified value.
        /// 
        /// Example: `?timestamp.le=2020-02-20T02:40:57Z`.
        /// </summary>
        public DateTime? Le { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of datetimes to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?timestamp.in=2020-02-20,2020-02-21`.
        /// </summary>
        public List<DateTime> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of datetimes to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?timestamp.ni=2020-02-20,2020-02-21`.
        /// </summary>
        public List<DateTime> Ni { get; set; }
    }
}
