using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class BlockCommit : ICommit<Block>
    {
        #region constants
        protected virtual int PreservedCycles => 5;

        protected virtual int BlocksPerCycle => 4096;
        protected virtual int BlocksPerCommitment => 32;
        protected virtual int BlocksPerSnapshot => 256;
        protected virtual int BlocksPerVoting => 32_768;

        protected virtual int TokensPerRoll => 10_000;

        protected virtual int ByteCost => 1000;
        protected virtual int OriginationCost => 257_000;
        protected virtual int NonceRevelationReward => 125_000;

        protected virtual int BlockDeposit => 0;
        protected virtual int EndorsementDeposit => 0;

        protected virtual int BlockReward => 0;
        protected virtual int EndorsementReward => 0;
        #endregion

        public Block Content { get; protected set; }
        public string NextProtocol { get; protected set; }

        protected readonly TzktContext Db;
        protected readonly AccountsCache Accounts;
        protected readonly ProtocolsCache Protocols;
        protected readonly StateCache State;

        public BlockCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Accounts = cache.Accounts;
            Protocols = cache.Protocols;
            State = cache.State;
        }

        public virtual async Task<BlockCommit> Init(JToken block)
        {
            await Validate(block);
            Content = await Parse(block);
            NextProtocol = block["metadata"]["next_protocol"].String();
            return this;
        }

        public virtual Task<BlockCommit> Init(Block block)
        {
            Content = block;
            return Task.FromResult(this);
        }

        public virtual async Task Apply()
        {
            #region balances
            var baker = Content.Baker;
            baker.Balance += BlockReward;
            baker.FrozenRewards += BlockReward;
            baker.FrozenDeposits += BlockDeposit;
            Db.Delegates.Update(baker);
            #endregion

            Db.Blocks.Add(Content);
            Protocols.ProtocolUp(Content.Protocol);
            await State.SetAppStateAsync(Content, NextProtocol);
        }

        public virtual async Task Revert()
        {
            #region balances
            var baker = Content.Baker;
            baker.Balance -= BlockReward;
            baker.FrozenRewards -= BlockReward;
            baker.FrozenDeposits -= BlockDeposit;
            Db.Delegates.Update(baker);
            #endregion

            Db.Blocks.Remove(Content);
            Protocols.ProtocolDown(Content.Protocol);
            await State.ReduceAppStateAsync();
        }

        public virtual async Task Validate(JToken block)
        {
            var currentBlock = await State.GetCurrentBlock();

            if (block["hash"] == null)
                throw new Exception($"Invalid block hash");

            if (block["header"]?["level"]?.Int32() != currentBlock.Level + 1)
                throw new Exception($"Invalid block level");

            if (block["protocol"] == null)
                throw new Exception($"Invalid block protocol");

            if (block["header"]?["timestamp"] == null)
                throw new Exception($"Invalid block timestamp");

            if (block["header"]?["priority"] == null)
                throw new Exception($"Invalid block priority");

            var baker = block["metadata"]?["baker"]?.ToString();
            if (!await Accounts.ExistsAsync(baker, AccountType.Delegate))
                throw new Exception($"Invalid block baker '{baker}'");
        }

        public virtual async Task<Block> Parse(JToken block)
        {
            return new Block
            {
                Hash = block["hash"].String(),
                Level = block["header"]["level"].Int32(),
                Protocol = await Protocols.GetProtocolAsync(block["protocol"].String()),
                Timestamp = block["header"]["timestamp"].DateTime(),
                Priority = block["header"]["priority"].Int32(),
                Baker = (Data.Models.Delegate)await Accounts.GetAccountAsync(block["metadata"]["baker"].String())
            };
        }
    }
}
