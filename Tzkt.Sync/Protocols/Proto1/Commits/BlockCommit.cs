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

        protected readonly TzktContext Db;
        protected readonly CacheService Cache;

        public BlockCommit(TzktContext db, CacheService cache)
        {
            Db = db;
            Cache = cache;
        }

        public virtual async Task<BlockCommit> Init(JToken block)
        {
            await Validate(block);
            Content = await Parse(block);
            return this;
        }

        public virtual Task<BlockCommit> Init(Block block)
        {
            Content = block;
            return Task.FromResult(this);
        }

        public virtual async Task Apply()
        {
            #region update balances
            var baker = Content.Baker;
            baker.Balance += BlockReward;
            baker.FrozenRewards += BlockReward;
            baker.FrozenDeposits += BlockDeposit;
            Db.Delegates.Update(baker);
            #endregion

            Db.Blocks.Add(Content);
            Cache.Protocols.ProtocolUp(Content.Protocol);
            await Cache.State.SetAppStateAsync(Content);
        }

        public virtual async Task Revert()
        {
            #region update balances
            var baker = Content.Baker;
            baker.Balance -= BlockReward;
            baker.FrozenRewards -= BlockReward;
            baker.FrozenDeposits -= BlockDeposit;
            Db.Delegates.Update(baker);
            #endregion

            Db.Blocks.Remove(Content);
            Cache.Protocols.ProtocolDown(Content.Protocol);
            await Cache.State.SetAppStateAsync(await Cache.State.GetPreviousBlock());
        }

        public virtual Task Validate(JToken block)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<Block> Parse(JToken block)
        {
            return new Block
            {
                Hash = block["hash"].String(),
                Level = block["header"]["level"].Int32(),
                Protocol = await Cache.Protocols.GetProtocolAsync(block["protocol"].String()),
                Timestamp = block["header"]["timestamp"].DateTime(),
                Priority = block["header"]["priority"].Int32(),
                Baker = (Delegate)await Cache.Accounts.GetAccountAsync(block["metadata"]["baker"].String())
            };
        }
    }
}
