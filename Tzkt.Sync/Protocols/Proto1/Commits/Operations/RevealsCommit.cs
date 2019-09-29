using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class RevealsCommit : ICommit<List<RevealOperation>>
    {
        public List<RevealOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;
        protected readonly Dictionary<string, string> PubKeys;

        public RevealsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;

            PubKeys = new Dictionary<string, string>(4);
        }

        public virtual async Task<RevealsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<RevealsCommit> Init(List<RevealOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var reveal in Content)
            {
                #region balances
                var baker = reveal.Block.Baker;
                var account = reveal.Sender;

                baker.FrozenFees += reveal.BakerFee;
                account.Balance -= reveal.BakerFee;
                #endregion

                #region counters
                account.Operations |= Operations.Reveals;
                account.Counter = Math.Max(account.Counter, reveal.Counter);
                #endregion

                if (account is User user)
                    user.PublicKey = PubKeys[account.Address];

                if (Db.Entry(baker).State != EntityState.Added)
                    Db.Delegates.Update(baker);

                if (Db.Entry(account).State != EntityState.Added)
                    Db.Accounts.Update(account);

                Db.RevealOps.Add(reveal);
            }

            return Task.CompletedTask;
        }

        public async Task Revert()
        {
            foreach (var reveal in Content)
            {
                #region balances
                var block = await State.GetCurrentBlock();
                var baker = (Data.Models.Delegate)await Accounts.GetAccountAsync(block.BakerId.Value);
                var account = await Accounts.GetAccountAsync(reveal.SenderId);

                baker.FrozenFees -= reveal.BakerFee;
                account.Balance += reveal.BakerFee;
                #endregion

                #region counters
                if (!await Db.RevealOps.AnyAsync(x => x.Sender.Id == account.Id && x.Id != reveal.Id))
                    account.Operations &= ~Operations.Reveals;

                account.Counter = Math.Min(account.Counter, reveal.Counter - 1);
                #endregion

                if (account is User user)
                    user.PublicKey = null;

                Db.Delegates.Update(baker);
                Db.Accounts.Update(account);
                Db.RevealOps.Remove(reveal);
            }
        }

        public async Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[3] ?? throw new Exception("Manager operations missed"))
            {
                operation.RequireValue("hash");
                operation.RequireArray("contents");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "reveal"))
                {
                    content.RequireValue("source");
                    content.RequireValue("fee");
                    content.RequireValue("counter");
                    content.RequireValue("gas_limit");
                    content.RequireValue("storage_limit");
                    content.RequireValue("public_key");
                    content.RequireObject("metadata");

                    var metadata = content["metadata"];
                    metadata.RequireArray("balance_updates");
                    metadata.RequireObject("operation_result");

                    var fee = content["fee"].Int64();
                    var src = content["source"].String();

                    var opUpdates = BalanceUpdates.Parse((JArray)metadata["balance_updates"]);
                    if ((fee == 0 && opUpdates.Count != 0) || (fee != 0 && opUpdates.Count != 2))
                        throw new Exception($"Invalid transaction balance updates count");

                    if (opUpdates.Count > 0)
                    {
                        if (!(opUpdates.FirstOrDefault(x => x is ContractUpdate) is ContractUpdate senderFeeUpdate) ||
                            !(opUpdates.FirstOrDefault(x => x is FeesUpdate) is FeesUpdate bakerFeeUpdate) ||
                            senderFeeUpdate.Change != -bakerFeeUpdate.Change ||
                            bakerFeeUpdate.Change != fee ||
                            senderFeeUpdate.Contract != src ||
                            bakerFeeUpdate.Delegate != block["metadata"]["baker"].String() ||
                            bakerFeeUpdate.Level != block["metadata"]["level"]["cycle"].Int32())
                            throw new Exception($"Invalid transaction fee balance updates");
                    }

                    var result = metadata["operation_result"];
                    result.RequireValue("status");

                    if (result["status"].String() != "applied")
                        throw new NotSupportedException();
                }
            }
        }

        public async Task<List<RevealOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<RevealOperation>();

            foreach (var operation in rawBlock["operations"][3])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "reveal"))
                {
                    var metadata = content["metadata"];
                    var opResult = metadata["operation_result"];

                    PubKeys[content["source"].String()] = content["public_key"].String();

                    result.Add(new RevealOperation
                    {
                        OpHash = opHash,
                        Block = parsedBlock,
                        Timestamp = parsedBlock.Timestamp,
                        BakerFee = content["fee"].Int64(),
                        Counter = content["counter"].Int32(),
                        GasLimit = content["gas_limit"].Int32(),
                        GasUsed = opResult["consumed_gas"]?.Int32() ?? 0,
                        StorageLimit = content["storage_limit"].Int32(),
                        Status = ParseStatus(metadata["operation_result"]["status"].String()),
                        Sender = await Accounts.GetAccountAsync(content["source"].String()),
                    });
                }
            }

            return result;
        }

        OperationStatus ParseStatus(string status) => status switch
        {
            "applied" => OperationStatus.Applied,
            _ => throw new NotImplementedException()
        };
    }
}
