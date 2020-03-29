using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SelectorBinder))]
    public class SelectorParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `select.eq=balance` is the same as `select=balance`). \
        /// Specify a comma-separated list field names to include into response.
        /// 
        /// Example: `?select=alias,address,balance,stakingBalance`.
        /// </summary>
        public List<string> Eq { get; set; }
    }
}
