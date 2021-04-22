using System.Collections.Generic;
using System.Text.Json;

namespace Tzkt.Sync.Protocols.Proto4
{
    class Validator : Proto3.Validator
    {
        public Validator(ProtocolHandler protocol) : base(protocol) { }

        // new RPC format
        protected override List<BalanceUpdate> ParseBalanceUpdates(IEnumerable<JsonElement> updates)
        {
            var res = new List<BalanceUpdate>(4);
            foreach (var update in updates)
            {
                res.Add(update.RequiredString("kind") switch
                {
                    "contract" => new BalanceUpdate
                    {
                        Kind = BalanceUpdateKind.Contract,
                        Account = update.RequiredString("contract"),
                        Change = update.RequiredInt64("change")
                    },
                    "freezer" => new BalanceUpdate
                    {
                        Kind = update.RequiredString("category") switch
                        {
                            "deposits" => BalanceUpdateKind.Deposits,
                            "rewards" => BalanceUpdateKind.Rewards,
                            "fees" => BalanceUpdateKind.Fees,
                            _ => throw new ValidationException("invalid freezer category")
                        },
                        Account = update.RequiredString("delegate"),
                        Change = update.RequiredInt64("change"),
                        Cycle = update.RequiredInt32("cycle"),
                    },
                    _ => throw new ValidationException("invalid balance update kind")
                });
            }
            return res;
        }
    }
}
