using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    [JsonConverter(typeof(SelectionSingleConverter))]
    public class SelectionSingleResponse
    {
        public required string[] Cols { get; set; }
        public required object?[] Vals { get; set; }
    }
}
