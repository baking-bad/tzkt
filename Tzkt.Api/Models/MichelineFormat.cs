using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tzkt.Api.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MichelineFormat
    {
        Json,
        JsonString,
        Raw,
        RawString,
    }
}
