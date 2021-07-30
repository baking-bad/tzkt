using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(OffsetBinder))]
    public class OffsetParameter
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
    }
}
