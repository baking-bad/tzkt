using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(StringBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class StringParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a string to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?parameters=abc`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a string to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?parameters.ne=abc`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **Same as** filter mode. \
        /// Specify a string template to get items where the specified field matches the specified template. \
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameters.as=*mid*` or `?parameters.as=*end`.
        /// </summary>
        public string As { get; set; }

        /// <summary>
        /// **Unlike** filter mode. \
        /// Specify a string template to get items where the specified field doesn't match the specified template.
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameters.un=*mid*` or `?parameters.un=*end`.
        /// </summary>
        public string Un { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is equal to one of the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?errors.in=bla,bal,abl`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is not equal to all the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?errors.ni=bla,bal,abl`.
        /// </summary>
        public List<string> Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?parameters.null` or `?parameters.null=false`.
        /// </summary>
        public bool? Null { get; set; }
    }
}
