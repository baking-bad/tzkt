using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(OffsetBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class OffsetParameter : INormalizable
    {
        /// <summary>
        /// **Elements** offset mode (optional, i.e. `offset.el=123` is the same as `offset=123`). \
        /// Skips specified number of elements.
        /// 
        /// Example: `?offset=100`.
        /// </summary>
        public int? El { get; set; }

        /// <summary>
        /// **Page** offset mode. \
        /// Skips `page * limit` elements. This is a classic pagination.
        /// 
        /// Example: `?offset.pg=1`.
        /// </summary>
        public int? Pg { get; set; }

        /// <summary>
        /// **Cursor** offset mode. \
        /// Skips all elements with the `cursor` before (including) the specified value. Cursor is a field used for sorting, e.g. `id`.
        /// Avoid using this offset mode with non-unique or non-sequential cursors such as `amount`, `balance`, etc.
        /// 
        /// Example: `?offset.cr=45837`.
        /// </summary>
        public long? Cr { get; set; }

        public static implicit operator OffsetParameter(int offset) => new() { El = offset };

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (El != null)
            {
                sb.Append($"offset.el={El}&");
            }

            if (Pg != null)
            {
                sb.Append($"offset.pg={Pg}&");
            }

            if (Cr != null)
            {
                sb.Append($"offset.cr={Cr}");
            }

            return sb.ToString();
        }
    }
}
