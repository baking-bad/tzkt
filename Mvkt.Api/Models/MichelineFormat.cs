using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mvkt.Api.Models
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
