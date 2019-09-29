using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class ActivationsCommit : ICommit<List<ActivationOperation>>
    {
        public List<ActivationOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public ActivationsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<ActivationsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<ActivationsCommit> Init(List<ActivationOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var activation in Content)
            {
                #region balances
                var account = activation.Account;

                account.Balance = activation.Balance;
                #endregion

                #region counters
                account.Operations |= Operations.Activations;
                #endregion

                Db.ActivationOps.Add(activation);
            }

            return Task.CompletedTask;
        }

        public async Task Revert()
        {
            foreach (var activation in Content)
            {
                var account = await Accounts.GetAccountAsync(activation.AccountId);

                Db.ActivationOps.Remove(activation);
                Db.Accounts.Remove(account);
            }
        }

        public async Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[2] ?? throw new Exception("Anonimous operations missed"))
            {
                operation.RequireValue("hash");
                operation.RequireArray("contents");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "activate_account"))
                {
                    content.RequireValue("pkh");
                    content.RequireObject("metadata");

                    var src = content["pkh"].String();
                    if (await Accounts.ExistsAsync(src, AccountType.User))
                        throw new Exception("Account is already activated");

                    var metadata = content["metadata"];
                    metadata.RequireArray("balance_updates");

                    var opUpdates = BalanceUpdates.Parse((JArray)metadata["balance_updates"]);
                    if (opUpdates.Count != 1)
                        throw new Exception($"Invalid activation balance updates count");

                    if (!(opUpdates[0] is ContractUpdate update) || update.Contract != content["pkh"].String())
                        throw new Exception($"Invalid activation balance updates");
                }
            }
        }

        public async Task<List<ActivationOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<ActivationOperation>();

            foreach (var operation in rawBlock["operations"][2])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "activate_account"))
                {
                    result.Add(new ActivationOperation
                    {
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        
                        OpHash = opHash,

                        Account = (User)await Accounts.GetAccountAsync(content["pkh"].String()),
                        Balance = content["metadata"]["balance_updates"][0]["change"].Int64()
                    });
                }
            }

            return result;
        }
    }
}
