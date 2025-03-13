using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    [JsonConverter(typeof(SelectionConverter))]
    public class SelectionResponse
    {
        public string[]? Cols { get; set; }
        public required object?[][] Rows { get; set; }
    }
}
