using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api
{
    [JsonConverter(typeof(SelectionConverter))]
    public class SelectionResponse
    {
        public string[] Cols { get; set; }
        public object[][] Rows { get; set; }
    }
}
