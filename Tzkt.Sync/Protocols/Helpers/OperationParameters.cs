using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Tzkt.Sync.Protocols
{
    static class OperationParameters
    {
        //static readonly JsonSerializerOptions Options = new JsonSerializerOptions { MaxDepth = 128; }
        
        public static string Parse(JsonElement parameters)
        {
            if (parameters.ValueKind == JsonValueKind.Undefined)
                return null;

            return JsonSerializer.Serialize(parameters);
        }
    }
}
