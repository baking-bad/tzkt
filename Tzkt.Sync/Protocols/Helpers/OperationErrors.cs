using System.Collections.Generic;
using System.Text.Json;
using Netezos.Contracts;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols
{
    static class OperationErrors
    {
        public static string Parse(JsonElement content, JsonElement errors)
        {
            if (errors.ValueKind == JsonValueKind.Undefined)
                return null;

            var res = new List<object>();
            
            foreach (var error in errors.EnumerateArray())
            {
                var id = error.GetProperty("id").GetString();
                var type = id[(id.IndexOf('.', id.IndexOf('.') + 1) + 1)..];

                res.Add(type switch
                {
                    "contract.balance_too_low" => new
                    {
                        type,
                        balance = long.Parse(error.GetProperty("balance").GetString()),
                        required = long.Parse(error.GetProperty("amount").GetString())
                    },
                    "contract.manager.unregistered_delegate" => new
                    {
                        type,
                        @delegate = error.GetProperty("hash").GetString()
                    },
                    "contract.non_existing_contract" => new
                    {
                        type,
                        contract = error.GetProperty("contract").GetString()
                    },
                    "Expression_already_registered" => new
                    {
                        type,
                        expression = ConstantSchema.GetGlobalAddress(Micheline.FromJson(content.Required("value")))
                    },
                    _ => new { type }
                });
            }

            return JsonSerializer.Serialize(res);
        }
    }
}
