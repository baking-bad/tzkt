using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class BigMapKeyFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? id { get; set; }

        /// <summary>
        /// Filter by bigmap ptr.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? bigmap { get; set; }

        /// <summary>
        /// Filters by status: `true` - active, `false` - removed.
        /// </summary>
        public bool? active { get; set; }

        /// <summary>
        /// Filter by key hash.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public ExpressionParameter? hash { get; set; }

        /// <summary>
        /// Filter by key.  
        /// Note, this parameter supports the following format: `key{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?key.foo.in=bar,baz`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter? key { get; set; }

        /// <summary>
        /// Filter by value.  
        /// Note, this parameter supports the following format: `value{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?value.foo.in=bar,baz`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter? value { get; set; }

        /// <summary>
        /// Filter by level of the block where the key was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the key was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the key was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the key was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter? lastTime { get; set; }

        /// <summary>
        /// Filter by number of actions with the bigmap key.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter? updates { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("bigmap", bigmap), ("active", active), ("hash", hash), ("key", key), ("value", value),
                ("firstLevel", firstLevel), ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime), ("updates", updates));
        }
    }
}
