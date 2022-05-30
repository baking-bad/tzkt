using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SelectBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class SelectParameter : INormalized
    {
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

        public string Normalize()
        {
            if (!Fields.Any() && !Values.Any())
                return "";

            var sb = new StringBuilder();

            if (Values.Any())
            {
                return $"select.values={string.Join(",", Values)}&";
            }
            else
            {
                return $"select.fields={string.Join(",", Fields)}&";
            }
        }
    }
}
