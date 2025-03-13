using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(Src1HashBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class Src1HashParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (`.eq` suffix can be omitted, i.e. `?param=...` is the same as `?param.eq=...`). \
        /// Specify an src1-hash to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?hash=src1qwe...`.
        /// </summary>
        public string? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify an src1-hash to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?hash.ne=src1qwe...`.
        /// </summary>
        public string? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of src1-hashes to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?hash.in=hash1,hash2,hash3`.
        /// </summary>
        public List<string>? In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of src1-hashes to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?hash.ni=hash1,hash2,hash3`.
        /// </summary>
        public List<string>? Ni { get; set; }

        #region operators
        public static implicit operator Src1HashParameter(string value) => new() { Eq = value };
        #endregion

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

            return sb.ToString();
        }
    }
}
