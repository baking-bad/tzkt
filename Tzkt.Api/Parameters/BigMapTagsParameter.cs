using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BigMapTagsBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "metadata,token_metadata,ledger")]
    public class BigMapTagsParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a comma-separated list of bigmap tags to get bigmaps with exactly the same set of tags.
        /// Avoid using this mode and use `.any` or `.all` instead, because it may not work as expected due to internal 'hidden' tags.
        /// 
        /// Example: `?tags=metadata` or `?tags=token_metadata,metadata`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Has any** filter mode. \
        /// Specify a comma-separated list of bigmap tags to get bigmaps where at least one of the specified tags is presented.
        /// 
        /// Example: `?tags.any=metadata` or `?tags.any=token_metadata,metadata`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Any { get; set; }

        /// <summary>
        /// **Has all** filter mode. \
        /// Specify a comma-separated list of bigmap tags to get bigmaps where all of the specified tags are presented.
        /// 
        /// Example: `?tags.all=metadata` or `?tags.all=token_metadata,metadata`.
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
