using System.Text.Json;
using Netezos.Contracts;
using Netezos.Encoding;

namespace Tzkt.Sync.Protocols
{
    static class OperationErrors
    {
        public static string? Parse(JsonElement content, JsonElement errors)
        {
            if (errors.ValueKind == JsonValueKind.Undefined)
                return null;

            var res = new List<object>();
            
            foreach (var error in errors.EnumerateArray())
            {
                var id = error.RequiredString("id");
                var type = id[(id.IndexOf('.', id.IndexOf('.') + 1) + 1)..];

                res.Add(type switch
                {
                    "contract.balance_too_low" => new
                    {
                        type,
                        balance = long.Parse(error.RequiredString("balance")),
                        required = long.Parse(error.RequiredString("amount"))
                    },
                    "contract.manager.unregistered_delegate" => new
                    {
                        type,
                        @delegate = error.RequiredString("hash")
                    },
                    "contract.non_existing_contract" => new
                    {
                        type,
                        contract = error.RequiredString("contract")
                    },
                    "Expression_already_registered" => new
                    {
                        type,
                        expression = ConstantSchema.GetGlobalAddress(content.RequiredMicheline("value"))
                    },
                    _ => new { type }
                });
            }

            return JsonSerializer.Serialize(res);
        }
    }
}
