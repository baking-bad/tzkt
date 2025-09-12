using System.Text.Json.Serialization;

namespace Mvkt.Api
{
    [JsonConverter(typeof(SelectionSingleConverter))]
    public class SelectionSingleResponse
    {
        public string[] Cols { get; set; }
        public object[] Vals { get; set; }
    }
}
