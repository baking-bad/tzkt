using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto24
{
    class TransactionsCommit(ProtocolHandler protocol) : Proto14.TransactionsCommit(protocol)
    {
        protected override async Task ApplyAddressRegistryDiffs(TransactionOperation transaction, JsonElement result)
        {
            if (result.TryGetProperty("address_registry_diff", out var diffs))
            {
                var minIndex = int.MaxValue;
                foreach (var diff in diffs.EnumerateArray())
                {
                    var address = diff.RequiredString("address");
                    var index = diff.RequiredInt32("index");

                    var account = await Cache.Accounts.GetOrCreateAsync(address);
                    if (account.Index != null)
                    {
                        if (account.Index != index)
                            throw new Exception("Address registry contains duplicates");

                        continue;
                    }

                    Db.TryAttach(account);
                    account.Index = index;

                    if (index < minIndex)
                        minIndex = index;
                }

                if (minIndex != int.MaxValue)
                    transaction.AddressRegistryIndex = minIndex;
            }
        }

        protected override async Task RevertAddressRegistryDiffs(TransactionOperation transaction)
        {
            if (transaction.AddressRegistryIndex is int minIndex)
            {
                var accounts = await Db.Accounts
                    .Where(x => x.Index != null && x.Index >= minIndex)
                    .ToListAsync();

                foreach (var account in accounts)
                {
                    Cache.Accounts.Add(account);
                    account.Index = null;
                }
            }
        }
    }
}
