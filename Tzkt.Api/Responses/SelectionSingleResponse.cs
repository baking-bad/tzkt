using System.Text.Json.Serialization;

namespace Tzkt.Api
{
    [JsonConverter(typeof(SelectionSingleConverter))]
    public class SelectionSingleResponse
    {
        public string[] Cols { get; set; }
        public object[] Vals { get; set; }
    }
}
