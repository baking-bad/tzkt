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
        /// **Id** offset mode. \
        /// Skips all elements with `id` before (including) the specified value. This is the most preferred way to enumerate items, especially backward.
        /// 
        /// Example: `?offset.id=45837`.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// **Page** offset mode. \
        /// Skips `page * limit` elements. This is a classic pagination.
        /// 
        /// Example: `?offset.pg=1`.
        /// </summary>
        public int? Pg { get; set; }
    }
}
