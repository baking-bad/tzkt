using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(ContractTagsBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "fa1,fa12,fa2")]
    public class ContractTagsParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a comma-separated list of contract tags to get contracts with exactly the same set of tags.
        /// Avoid using this mode and use `.any` or `.all` instead, because it may not work as expected due to internal 'hidden' tags.
        /// 
        /// Example: `?tags=fa2` or `?tags=fa1,fa12`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Has any** filter mode. \
        /// Specify a comma-separated list of contract tags to get contracts where at least one of the specified tags is presented.
        /// 
        /// Example: `?tags.any=fa2` or `?tags.any=fa1,fa12`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Any { get; set; }

        /// <summary>
        /// **Has all** filter mode. \
        /// Specify a comma-separated list of contract tags to get contracts where all of the specified tags are presented.
        /// 
        /// Example: `?tags.all=fa2` or `?tags.all=fa1,fa12`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? All { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();
            
            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq}&");
            }
            
            if (Any != null)
            {
                sb.Append($"{name}.any={Any}&");
            }
            
            if (All != null)
            {
                sb.Append($"{name}.all={All}&");
            }
            
            return sb.ToString();
        }
    }
}
