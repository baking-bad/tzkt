using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(VoterStatusBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "none,upvoted,voted_yay,voted_nay,voted_pass")]
    public class VoterStatusParameter : INormalized
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a voter status to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?status=none`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a voter status to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?status.ne=none`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of voter statuses to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?status.in=voted_yay,voted_nay`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of voter statuses to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?status.ni=none,upvoted`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int> Ni { get; set; }

        public string Normalize(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
