using System.Text;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AccountWithEntrypointBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class AccountWithEntrypointParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a `tz` or `KT` address with optional entrypoint to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?sender=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public (int, byte[]?)? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a `tz` or `KT` address with optional entrypoint to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?sender.ne=tz1WnfXMPaNTBmH7DBPwqCWs9cPDJdkGBTZ8%deposit`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public (int, byte[]?)? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of addresses with optional entrypoints to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=tz1WnfXMPaNTBWnfXMPaNTBWnfXMPaNTBNTB,tz1SiPXX4MYGNJNDSiPXX4MYGNJNDSiPXX4M%`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(int, byte[]?)>? In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of addresses with optional entrypoints to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=tz1WnfXMPaNTBWnfXMPaNTBWnfXMPaNTBNTB,tz1SiPXX4MYGNJNDSiPXX4MYGNJNDSiPXX4M%deposit`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<(int, byte[]?)>? Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?initiator.null` or `?initiator.null=false`.
        /// </summary>
        public bool? Null { get; set; }

        [JsonIgnore]
        public bool InHasNull { get; set; }

        [JsonIgnore]
        public bool NiHasNull { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            static string toString((int, byte[]?) address) => address.Item2 != null
                ? $"{address.Item1}%{Hex.Convert(address.Item2)}"
                : $"{address.Item1}";

            if (Eq != null)
            {
                sb.Append($"{name}.eq={toString(Eq.Value)}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={toString(Ne.Value)}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.Select(toString).OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.Select(toString).OrderBy(x => x))}&");
            }

            if (Null != null)
            {
                sb.Append($"{name}.null={Null}&");
            }

            sb.Append($"{name}.NiHasNull={NiHasNull}&");
            sb.Append($"{name}.InHasNull={InHasNull}&");

            return sb.ToString();
        }
    }
}
