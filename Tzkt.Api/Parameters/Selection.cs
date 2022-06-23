using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class Selection : INormalizable
    {
        /// <summary>
        /// Specify a comma-separated list of fields to include into response or leave it undefined to get default set of fields.
        /// This parameter accepts values of the following format: `{field}{path?}{as alias?}`, so you can do deep selection
        /// (for example, `?select=balance,token.metadata.symbol as token,...`).  
        /// Note, if you select just one field, the response will be flatten into a simple array of values.  
        /// Click on the parameter to expand the details.
        /// </summary>
        public SelectionParameter select { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("", ("select", select));
        }
    }
}
