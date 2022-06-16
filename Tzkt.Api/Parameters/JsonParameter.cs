using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

using Tzkt.Api.Utils;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(JsonBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
    public class JsonParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a JSON value to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?parameter.from=tz1...` or `?parameter.signatures.[3].[0]=null` or `?parameter.sigs.[*]=null`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a JSON value to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?parameter.ne=true` or `?parameter.amount.ne=0`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Ne { get; set; }

        /// <summary>
        /// **Greater than** filter mode. \
        /// Specify a string to get items where the specified field is greater than the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.gt=1234` or `?parameter.time.gt=2021-02-01`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Gt { get; set; }

        /// <summary>
        /// **Greater or equal** filter mode. \
        /// Specify a string to get items where the specified field is greater than equal to the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.ge=1234` or `?parameter.time.ge=2021-02-01`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Ge { get; set; }

        /// <summary>
        /// **Less than** filter mode. \
        /// Specify a string to get items where the specified field is less than the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.lt=1234` or `?parameter.time.lt=2021-02-01`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Lt { get; set; }

        /// <summary>
        /// **Less or equal** filter mode. \
        /// Specify a string to get items where the specified field is less than or equal to the specified value.
        /// Note that all stored JSON values are strings, so this will be a comparison of two strings, so we recommend comparing values of the same type,
        /// e.g. numeric strings with numeric strings (`parameter.number.gt=123`), datetime strings with datetime strings (`parameter.date.gt=2021-01-01`), etc.
        /// Otherwise, result may surprise you.
        /// 
        /// Example: `?parameter.balance.le=1234` or `?parameter.time.le=2021-02-01`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Le { get; set; }

        /// <summary>
        /// **Same as** filter mode. \
        /// Specify a string template to get items where the specified field matches the specified template. \
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameter.as=*mid*` or `?parameter.as=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> As { get; set; }

        /// <summary>
        /// **Unlike** filter mode. \
        /// Specify a string template to get items where the specified field doesn't match the specified template.
        /// This mode supports wildcard `*`. Use `\*` as an escape symbol.
        /// 
        /// Example: `?parameter.un=*mid*` or `?parameter.un=*end`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string)> Un { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of strings or JSON array to get items where the specified field is equal to one of the specified values. \
        /// 
        /// Example: `?parameter.amount.in=1,2,3` or `?parameter.in=[{"from":"tz1","to":"tz2"},{"from":"tz2","to":"tz1"}]`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string[])> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of strings to get items where the specified field is not equal to all the specified values. \
        /// Use `\,` as an escape symbol.
        /// 
        /// Example: `?parameter.amount.ni=1,2,3` or `?parameter.ni=[{"from":"tz1","to":"tz2"},{"from":"tz2","to":"tz1"}]`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], string[])> Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?parameter.null` or `?parameter.null=false` or `?parameter.sigs.[0].null=false`.
        /// </summary>
        [JsonSchemaType(typeof(bool))]
        [JsonSchemaExtensionData("x-tzkt-extension", "json-parameter")]
        public List<(JsonPath[], bool)> Null { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                foreach (var (path, value) in Eq)
                {
                    sb.Append($"{name}.{Normalize(path)}.eq={value}&");
                }
            }

            if (Ne != null)
            {
                foreach (var (path, value) in Ne)
                {
                    sb.Append($"{name}.{Normalize(path)}.ne={value}&");
                }
            }

            if (Gt != null)
            {
                foreach (var (path, value) in Gt)
                {
                    sb.Append($"{name}.{Normalize(path)}.gt={value}&");
                }
            }

            if (Ge != null)
            {
                foreach (var (path, value) in Ge)
                {
                    sb.Append($"{name}.{Normalize(path)}.ge={value}&");
                }
            }

            if (Lt != null)
            {
                foreach (var (path, value) in Lt)
                {
                    sb.Append($"{name}.{Normalize(path)}.lt={value}&");

                }
            }

            if (Le != null)
            {
                foreach (var (path, value) in Le)
                {
                    sb.Append($"{name}.{Normalize(path)}.le={value}&");

                }
            }

            if (As != null)
            {
                foreach (var (path, value) in As)
                {
                    sb.Append($"{name}.{Normalize(path)}.as={value}&");
                }
            }

            if (Un != null)
            {
                foreach (var (path, value) in Un)
                {
                    sb.Append($"{name}.{Normalize(path)}.un={value}&");

                }
            }

            if (In != null)
            {
                foreach (var (path, values) in In)
                {
                    sb.Append($"{name}.{Normalize(path)}.in={string.Join(",", values.OrderBy(x => x))}&");
                }
            }

            if (Ni != null)
            {
                foreach (var (path, values) in Ni)
                {
                    sb.Append($"{name}.{Normalize(path)}.ni={string.Join(",", values.OrderBy(x => x))}&");
                }
            }

            if (Null != null)
            {
                foreach (var (path, value) in Null)
                {
                    sb.Append($"{name}.{Normalize(path)}.null={value}&");
                }
            }

            return sb.ToString();
        }

        static string Normalize(JsonPath[] jsonPaths)
        {
            return string.Join(".", jsonPaths.Select(x => x.Type > JsonPathType.Key ? $"[{x.Value ?? "*"}]" : x.Value));
        }
    }
}