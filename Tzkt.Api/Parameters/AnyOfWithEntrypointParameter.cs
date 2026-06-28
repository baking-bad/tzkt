using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AnyOfWithEntrypointBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "anyof-parameter")]
    public class AnyOfWithEntrypointParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a value to get items where any of the specified fields is equal to the specified value.
        /// 
        /// Example: `?anyof.sender.target=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public (int, byte[]?)? Eq { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of values to get items where any of the specified fields is equal to one of the specified values.
        /// 
        /// Example: `?anyof.sender.target.in=tz1WnfXMPaNTBWnfXMPaNTBWnfXMPaNTBNTB,tz1SiPXX4MYGNJNDSiPXX4MYGNJNDSiPXX4M%entrypoint,null`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(int, byte[]?)>? In { get; set; }

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
        public required IEnumerable<string> Fields { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            static string toString((int, byte[]?) address) => address.Item2 != null
                ? $"{address.Item1}%{Hex.Convert(address.Item2)}"
                : $"{address.Item1}";

            if (Eq != null)
            {
                sb.Append($"{name}.{string.Join(".", Fields)}.eq={toString(Eq.Value)}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.{string.Join(".", Fields)}.in={string.Join(",", In.Select(toString).OrderBy(x => x))}&");
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