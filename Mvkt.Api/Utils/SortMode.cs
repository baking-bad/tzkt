using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mvkt.Api
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SortMode
    {
        Ascending = 0,
        Descending = 1
    }
}
