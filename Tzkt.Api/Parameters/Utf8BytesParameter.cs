using System.Text;
using Microsoft.AspNetCore.Mvc;
using Netezos.Encoding;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(Utf8BytesBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class Utf8BytesParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a UTF8 string to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?entrypoint=abc`.
        /// </summary>
        public byte[]? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a UTF8 string to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?entrypoint.ne=abc`.
        /// </summary>
        public byte[]? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of UTF8 strings to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?entrypoint.in=bla,bal,abl`.
        /// </summary>
        public List<byte[]>? In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of UTF8 strings to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?entrypoint.ni=bla,bal,abl`.
        /// </summary>
        public List<byte[]>? Ni { get; set; }

        /// <summary>
        /// **Is null** filter mode. \
        /// Use this mode to get items where the specified field is null or not.
        /// 
        /// Example: `?entrypoint.null` or `?entrypoint.null=false`.
        /// </summary>
        public bool? Null { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Hex.Convert(Eq)}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={Hex.Convert(Ne)}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.Select(Hex.Convert).OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.Select(Hex.Convert).OrderBy(x => x))}&");
            }

            if (Null != null)
            {
                sb.Append($"{name}.null={Null}&");
            }

            return sb.ToString();
        }
    }
}
