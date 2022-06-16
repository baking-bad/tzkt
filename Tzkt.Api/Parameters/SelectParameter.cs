using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SelectBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class SelectParameter : INormalizable
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

        public string Normalize(string name)
        {
            //TODO: we can't order values, but perhaps we can order fields.
            return Values != null ? $"select.values={string.Join(",", Values)}&" : $"select.fields={string.Join(",", Fields)}&";
        }
    }
}
