using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AnyOfBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "anyof-parameter")]
    public class AnyOfParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a value to get items where any of the specified fields is equal to the specified value.
        /// 
        /// Example: `?anyof.sender.target=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of values to get items where any of the specified fields is equal to one of the specified values.
        /// 
        /// Example: `?anyof.sender.target.in=tz1WnfXMPaNTBWnfXMPaNTBWnfXMPaNTBNTB,tz1SiPXX4MYGNJNDSiPXX4MYGNJNDSiPXX4M,null`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where any of the specified fields is null or not.
        /// 
        /// Example: `?anyof.from.to.null` or `?anyof.from.to.null=false`.
        /// </summary>
        public bool? Null { get; set; }

        [JsonIgnore]
        public bool InHasNull { get; set; }

        [JsonIgnore]
        public IEnumerable<string> Fields { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.{string.Join(".", Fields)}.eq={Eq}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.{string.Join(".", Fields)}.in={string.Join(",", In.OrderBy(x => x))}&");
            }

            if (Null != null)
            {
                sb.Append($"{name}.{string.Join(".", Fields)}.null={Null}&");
            }

            sb.Append($"{name}.{string.Join(".", Fields)}.InHasNull={InHasNull}&");

            return sb.ToString();
        }
    }
}