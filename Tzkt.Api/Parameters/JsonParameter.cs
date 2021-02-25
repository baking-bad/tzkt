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
        /// Example: `?parameter={}` or `?parameter.to=tz1...`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a string to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?parameter.ne={}` or `?parameter.amount.ne=0`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify a string to get items where the specified field is greater than the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.gt=1234` or `?parameter.time.gt=2021-02-01`.
        /// </summary>
        public List<(string, string)> Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify a string to get items where the specified field is greater than equal to the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.ge=1234` or `?parameter.time.ge=2021-02-01`.
        /// </summary>
        public List<(string, string)> Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify a string to get items where the specified field is less than the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.lt=1234` or `?parameter.time.lt=2021-02-01`.
        /// </summary>
        public List<(string, string)> Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify a string to get items where the specified field is less than or equal to the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.le=1234` or `?parameter.time.le=2021-02-01`.
        /// </summary>
        public List<(string, string)> Le { get; set; }

        /// <summary>
        /// **Same as** filter mode. \
        /// Specify a string template to get items where the specified field matches the specified template. \
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameter.as=*mid*` or `?parameter.as=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> As { get; set; }

        /// <summary>
        /// **Unlike** filter mode. \
        /// Specify a string template to get items where the specified field doesn't match the specified template.
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameter.un=*mid*` or `?parameter.un=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public List<(string, string)> Un { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is equal to one of the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?parameter.in=bla,bal,abl` or `?parameter.from.in=tz1,tz2,tz3`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(string, List<string>)> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is not equal to all the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?parameter.ni=bla,bal,abl` or `?parameter.from.ni=tz1,tz2,tz3`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(string, List<string>)> Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?parameter.null` or `?parameter.null=false` or `?parameter.sigs.0.null=false`.
        /// </summary>
        [JsonSchemaType(typeof(bool))]
        public List<(string, bool)> Null { get; set; }
    }
}
