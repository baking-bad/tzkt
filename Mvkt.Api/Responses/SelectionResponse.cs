using System.Text.Json.Serialization;

namespace Mvkt.Api
{
    [JsonConverter(typeof(SelectionConverter))]
    public class SelectionResponse
    {
        public string[] Cols { get; set; }
        public object[][] Rows { get; set; }
    }
}
