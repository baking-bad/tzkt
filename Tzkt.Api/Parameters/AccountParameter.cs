using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AccountBinder))]
    [JsonSchemaExtensionData("x-mvkt-extension", "query-parameter")]
    public class AccountParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a `tz` or `KT` address to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?sender=mv1VXHDHLA9dAKA8vpGuU7P8GXCVqSJ99obq`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a `tz` or `KT` address to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?sender.ne=mv1VXHDHLA9dAKA8vpGuU7P8GXCVqSJ99obq`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of addresses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=mv1HeozQdwBYmdjaKoTaGfLc4qxYWkCTSEtL,mv1FABmn7c7rKWKgKh5RYBVQkBSv3sET36zt`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of addresses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=mv1HeozQdwBYmdjaKoTaGfLc4qxYWkCTSEtL,mv1FABmn7c7rKWKgKh5RYBVQkBSv3sET36zt`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }

        /// <summary>
        /// **Equal to another field** filter mode. \
        /// Specify a field name to get items where the specified fields are equal.
        /// 
        /// Example: `?sender.eqx=target`.
        /// </summary>
        public string Eqx { get; set; }

        /// <summary>
        /// **Not equal to another field** filter mode. \
        /// Specify a field name to get items where the specified fields are not equal.
        /// 
        /// Example: `?sender.nex=initiator`.
        /// </summary>
        public string Nex { get; set; }

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

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={Ne}&");
            }

            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.OrderBy(x => x))}&");
            }

            if (Eqx != null)
            {
                sb.Append($"{name}.eqx={Eqx}&");
            }

            if (Nex != null)
            {
                sb.Append($"{name}.nex={Nex}&");
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
