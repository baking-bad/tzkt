using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(Int64Binder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class Int64Parameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify an integer number to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?balance=1234`.
        /// </summary>
        public long? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an integer number to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?balance.ne=1234`.
        /// </summary>
        public long? Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify an integer number to get items where the specified field is greater than the specified value.
        /// 
        /// Example: `?balance.gt=1234`.
        /// </summary>
        public long? Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify an integer number to get items where the specified field is greater than equal to the specified value.
        /// 
        /// Example: `?balance.ge=1234`.
        /// </summary>
        public long? Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify an integer number to get items where the specified field is less than the specified value.
        /// 
        /// Example: `?balance.lt=1234`.
        /// </summary>
        public long? Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify an integer number to get items where the specified field is less than or equal to the specified value.
        /// 
        /// Example: `?balance.le=1234`.
        /// </summary>
        public long? Le { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of integers to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?level.in=12,14,52,69`.
        /// </summary>
        public List<long> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of integers to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?level.ni=12,14,52,69`.
        /// </summary>
        public List<long> Ni { get; set; }
    }
}
