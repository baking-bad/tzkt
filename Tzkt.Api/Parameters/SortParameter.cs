using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SortBinder))]
    public class SortParameter
    {
        /// <summary>
        /// **Ascending** sort mode (optional, i.e. `sort.asc=id` is the same as `sort=id`). \
        /// Specify a field name to sort by.
        /// 
        /// Example: `?sort=balance`.
        /// </summary>
        public string Asc { get; set; }

        /// <summary>
        /// **Descending** sort mode. \
        /// Specify a field name to sort by descending.
        /// 
        /// Example: `?sort.desc=id`.
        /// </summary>
        public string Desc { get; set; }
    }
}
