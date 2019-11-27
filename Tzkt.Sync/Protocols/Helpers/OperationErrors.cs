using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Tzkt.Sync.Protocols
{
    static class OperationErrors
    {
        public static string Parse(JsonElement errors)
        {
            if (errors.ValueKind == JsonValueKind.Undefined)
                return null;

            var res = new List<object>();
            
            foreach (var error in errors.EnumerateArray())
            {
                var id = error.GetProperty("id").GetString();
                var code = id.Substring(id.IndexOf('.', id.IndexOf('.') + 1) + 1);

                res.Add(code switch
                {
                    "contract.balance_too_low" => new 
                    { 
                        code,
                        balance = long.Parse(error.GetProperty("balance").GetString())
                    },
                    "contract.manager.unregistered_delegate" => new
                    {
                        code,
                        @delegate = error.GetProperty("hash").GetString()
                    },
                    "contract.non_existing_contract" => new
                    {
                        code,
                        contract = error.GetProperty("contract").GetString()
                    },
                    _ => new { code }
                });
            }

            return JsonSerializer.Serialize(res);
        }
    }
}
