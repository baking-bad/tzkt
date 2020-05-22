using Microsoft.AspNetCore.Mvc;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SelectBinder))]
    public class SelectParameter
    {
#pragma warning disable CA1819 // Properties should not return arrays
        /// <summary>
        /// **Fields** selection mode (optional, i.e. `select.fields=balance` is the same as `select=balance`). \
        /// Specify a comma-separated list of fields to include into response.
        /// 
        /// Example: `?select=address,balance` => `[ { "address": "asd", "balance": 10 } ]`.
        /// </summary>
        public string[] Fields { get; set; }

        /// <summary>
        /// **Values** selection mode. \
        /// Specify a comma-separated list of fields to include their values into response.
        /// 
        /// Example: `?select.values=address,balance` => `[ [ "asd", 10 ] ]`.
        /// </summary>
        public string[] Values { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
