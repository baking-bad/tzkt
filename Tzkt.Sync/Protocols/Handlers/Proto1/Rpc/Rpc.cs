using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Rpc : IRpc
    {
        protected readonly TezosNode Node;

        public Rpc(TezosNode node) => Node = node;

        #region indexer
        public virtual Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public virtual Task<JsonElement> GetCycleAsync(int level, int cycle)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/cycle/{cycle}");

        public virtual Task<JsonElement> GetBakingRightsAsync(int block, int cycle)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/baking_rights?cycle={cycle}&max_priority=8&all=true");

        public virtual Task<JsonElement> GetEndorsingRightsAsync(int block, int cycle)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/endorsing_rights?cycle={cycle}");

        public virtual Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxPriority)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/baking_rights?level={level}&max_priority={maxPriority + 1}&all=true");

        public virtual Task<JsonElement> GetLevelEndorsingRightsAsync(int block, int level)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/endorsing_rights?level={level}");

        public virtual Task<JsonElement> GetContractAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/contracts/{address}");

        public virtual Task<JsonElement> GetDelegateAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/delegates/{address}");
        #endregion

        #region bootstrap
        public virtual Task<JsonElement> GetAllContractsAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/contracts/index?depth=1");
        #endregion

        #region diagnostics
        public virtual Task<JsonElement> GetGlobalCounterAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/contracts/global_counter");
        #endregion
    }
}
