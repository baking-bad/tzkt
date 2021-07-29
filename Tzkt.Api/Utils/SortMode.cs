using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tzkt.Api
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SortMode
    {
        Ascending = 0,
        Descending = 1
    }
}
