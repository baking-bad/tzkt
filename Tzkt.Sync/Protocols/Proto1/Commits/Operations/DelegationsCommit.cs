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
    public class DelegationsCommit : ICommit<List<DelegationOperation>>
    {
        public List<DelegationOperation> Content { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public DelegationsCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<DelegationsCommit> Init(JToken rawBlock, Block parsedBlock)
        {
            await Validate(rawBlock);
            Content = await Parse(rawBlock, parsedBlock);
            return this;
        }

        public virtual Task<DelegationsCommit> Init(List<DelegationOperation> operations)
        {
            Content = operations;
            return Task.FromResult(this);
        }

        public Task Apply()
        {
            foreach (var delegation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public Task Revert()
        {
            foreach (var delegation in Content)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public async Task Validate(JToken block)
        {
            foreach (var operation in block["operations"]?[3] ?? throw new Exception("Manager operations missed"))
            {
                operation.RequireValue("hash");
                operation.RequireArray("contents");

                foreach (var content in operation["contents"]
                    .Where(x => (x["kind"]?.String() ?? throw new Exception("Invalid content kind")) == "delegation"))
                {
                    content.RequireValue("source");
                    content.RequireValue("fee");
                    content.RequireValue("counter");
                    content.RequireValue("gas_limit");
                    content.RequireValue("storage_limit");
                    content.RequireValue("delegate");
                    content.RequireObject("metadata");

                    var metadata = content["metadata"];
                    metadata.RequireArray("balance_updates");
                    metadata.RequireObject("operation_result");

                    var fee = content["fee"].Int64();
                    var src = content["source"].String();

                    if (!await Accounts.ExistsAsync(src))
                        throw new Exception("Unknown source account");

                    var opUpdates = BalanceUpdates.Parse((JArray)metadata["balance_updates"]);
                    if ((fee == 0 && opUpdates.Count != 0) || (fee != 0 && opUpdates.Count != 2))
                        throw new Exception($"Invalid delegation balance updates count");

                    if (opUpdates.Count > 0)
                    {
                        if (!(opUpdates.FirstOrDefault(x => x is ContractUpdate) is ContractUpdate senderFeeUpdate) ||
                            !(opUpdates.FirstOrDefault(x => x is FeesUpdate) is FeesUpdate bakerFeeUpdate) ||
                            senderFeeUpdate.Change != -bakerFeeUpdate.Change ||
                            bakerFeeUpdate.Change != fee ||
                            senderFeeUpdate.Contract != src ||
                            bakerFeeUpdate.Delegate != block["metadata"]["baker"].String() ||
                            bakerFeeUpdate.Level != block["metadata"]["level"]["cycle"].Int32())
                            throw new Exception($"Invalid delegation fee balance updates");
                    }

                    var result = metadata["operation_result"];
                    result.RequireValue("status");

                    if (result["status"].String() != "applied")
                        throw new NotSupportedException();

                    var delegat = content["delegate"]?.String();
                    if (delegat != null)
                    {
                        if (src != delegat && !await Accounts.ExistsAsync(delegat, AccountType.Delegate))
                            throw new Exception("Unknown delegate account");

                        var delegatAccount = await Accounts.GetAccountAsync(delegat);
                        if (src == delegat && delegatAccount is User)
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public async Task<List<DelegationOperation>> Parse(JToken rawBlock, Block parsedBlock)
        {
            var result = new List<DelegationOperation>();

            foreach (var operation in rawBlock["operations"][3])
            {
                var opHash = operation["hash"].String();

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "delegation"))
                    result.Add(await ParseDelegation(parsedBlock, opHash, content));

                foreach (var content in operation["contents"].Where(x => x["kind"].String() == "transaction" && x["metadata"]["internal_operation_results"] != null))
                    foreach (var internalContent in content["metadata"]["internal_operation_results"].Where(x => x["kind"].String() == "delegation"))
                        result.Add(await ParseInternalDelegation(parsedBlock, opHash, content, internalContent));
            }

            return result;
        }

        async Task<DelegationOperation> ParseDelegation(Block block, string opHash, JToken content)
        {
            var metadata = content["metadata"];
            var opResult = metadata["operation_result"];

            return new DelegationOperation
            {
                Block = block,
                Timestamp = block.Timestamp,

                OpHash = opHash,

                BakerFee = content["fee"].Int64(),
                Counter = content["counter"].Int32(),
                GasLimit = content["gas_limit"].Int32(),
                StorageLimit = content["storage_limit"].Int32(),
                Sender = await Accounts.GetAccountAsync(content["source"].String()),
                Delegate = content["delegate"] != null
                    ? (Data.Models.Delegate)await Accounts.GetAccountAsync(content["delegate"].String())
                    : null,

                Status = opResult["status"].OperationStatus(),
            };
        }

        async Task<DelegationOperation> ParseInternalDelegation(Block block, string opHash, JToken parent, JToken content)
        {
            var metadata = content["metadata"];
            var opResult = metadata["operation_result"];

            return new DelegationOperation
            {
                Block = block,
                Timestamp = block.Timestamp,

                OpHash = opHash,

                Counter = parent["counter"].Int32(),

                Nonce = content["nonce"].Int32(),
                Sender = await Accounts.GetAccountAsync(content["source"].String()),
                Delegate = content["delegate"] != null
                    ? (Data.Models.Delegate)await Accounts.GetAccountAsync(content["delegate"].String())
                    : null,

                Status = opResult["status"].OperationStatus(),
            };
        }
    }
}
