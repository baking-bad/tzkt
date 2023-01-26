using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class DomainFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal TzKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter id { get; set; }

        /// <summary>
        /// Filter by the domain level.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter level { get; set; }

        /// <summary>
        /// Filter by the domain name.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public StringParameter name { get; set; }

        /// <summary>
        /// Filter by the domain owner.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AddressNullParameter owner { get; set; }

        /// <summary>
        /// Filter by the address the domain points to.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AddressNullParameter address { get; set; }

        /// <summary>
        /// Filter by the 'reverse' flag.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public BoolParameter reverse { get; set; }

        /// <summary>
        /// Filter by the domain expiration.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public DateTimeParameter expiration { get; set; }

        /// <summary>
        /// Filter by the domain data.  
        /// Note, this parameter supports the following format: `data{.path?}{.mode?}=...`,
        /// so you can specify a path to a particular field to filter by (for example, `?data.foo=bar`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public JsonParameter data { get; set; }

        /// <summary>
        /// Filter by level of the block where the domain was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the domain was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the domain was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastLevel { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the domain was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastTime { get; set; }

        public string Normalize(string prop)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("level", level), ("name", name), ("owner", owner), ("address", address),
                ("reverse", reverse), ("expiration", expiration), ("data", data), ("firstLevel", firstLevel),
                ("firstTime", firstTime), ("lastLevel", lastLevel), ("lastTime", lastTime));
        }
    }
}
