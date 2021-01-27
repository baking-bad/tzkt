using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(JsonBinder))]
    public class JsonParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a string to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?params={}` or `?params.to=tz1...`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a string to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?params.ne={}` or `?params.amount.ne=0`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Ne { get; set; }

        /// <summary>
        /// **Same as** filter mode. \
        /// Specify a string template to get items where the specified field matches the specified template. \
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?params.as=*mid*` or `?params.as=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> As { get; set; }

        /// <summary>
        /// **Unlike** filter mode. \
        /// Specify a string template to get items where the specified field doesn't match the specified template.
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?params.un=*mid*` or `?params.un=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Un { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is equal to one of the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?params.in=bla,bal,abl` or `?params.from.in=tz1,tz2,tz3`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(string, List<string>)> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is not equal to all the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?params.ni=bla,bal,abl` or `?params.from.ni=tz1,tz2,tz3`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(string, List<string>)> Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?params.null` or `?params.null=false` or `?params.sigs.0.null=false`.
        /// </summary>
        [JsonSchemaType(typeof(bool))]
        public List<(string, bool)> Null { get; set; }
    }
}
