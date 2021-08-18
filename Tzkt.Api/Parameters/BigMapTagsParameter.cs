using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(BigMapTagsBinder))]
    public class BigMapTagsParameter
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a comma-separated list of bigmap tags to get bigmaps with exactly the same set of tags.
        /// 
        /// Example: `?tags=metadata` or `?tags=token_metadata,metadata`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Has any** filter mode. \
        /// Specify a comma-separated list of bigmap tags to get bigmaps where at least one of the specified tags is presented.
        /// 
        /// Example: `?tags=metadata` or `?tags=token_metadata,metadata`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? Any { get; set; }

        /// <summary>
        /// **Has all** filter mode. \
        /// Specify a comma-separated list of bigmap tags to get bigmaps where all of the specified tags are presented.
        /// 
        /// Example: `?tags=metadata` or `?tags=token_metadata,metadata`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public int? All { get; set; }
    }
}
