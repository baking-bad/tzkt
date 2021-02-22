using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api
{
    [JsonConverter(typeof(SelectionSingleConverter))]
    public class SelectionSingleResponse
    {
        public string[] Cols { get; set; }
        public object[] Vals { get; set; }
    }
}
