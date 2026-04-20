using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto24
{
    class Rpc(TezosNode node) : IRpc
    {
        protected readonly TezosNode Node = node;

        #region indexer
        public virtual Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public virtual Task<JsonElement> GetContractAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/contracts/{address}");

        public virtual Task<JsonElement> GetContractManagerKeyAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/contracts/{address}/manager_key");

        public virtual Task<JsonElement> GetConstantsAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/constants");

        public virtual Task<JsonElement> GetContractsAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/contracts");
        #endregion
    }
}
