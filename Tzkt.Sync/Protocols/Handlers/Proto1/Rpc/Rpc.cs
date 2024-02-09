using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    class Rpc : IRpc
    {
        protected readonly MavrykNode Node;

        public Rpc(MavrykNode node) => Node = node;

        #region indexer
        public virtual Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public virtual Task<JsonElement> GetBakingRightsAsync(int block, int cycle)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/baking_rights?cycle={cycle}&max_priority=8&all=true");

        public virtual Task<JsonElement> GetEndorsingRightsAsync(int block, int cycle)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/endorsing_rights?cycle={cycle}");

        public virtual Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxRound)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/baking_rights?level={level}&max_priority={maxRound + 1}&all=true");

        public virtual Task<JsonElement> GetLevelEndorsingRightsAsync(int block, int level)
            => Node.GetAsync($"chains/main/blocks/{block}/helpers/endorsing_rights?level={level}");

        public virtual Task<JsonElement> GetContractAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/contracts/{address}");

        public virtual Task<JsonElement> GetDelegateAsync(int level, string address)
            => Node.GetAsync($"chains/main/blocks/{level}/context/delegates/{address}");

        public virtual Task<JsonElement> GetStakeDistribution(int block, int cycle)
            => throw new InvalidOperationException();

        public virtual Task<JsonElement> GetExpectedIssuance(int block)
            => throw new InvalidOperationException();
        #endregion

        #region diagnostics
        public virtual Task<JsonElement> GetGlobalCounterAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/contracts/global_counter");

        public virtual Task<JsonElement> GetDelegatesAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/delegates");

        public virtual Task<JsonElement> GetActiveDelegatesAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}/context/delegates?active=true");

        public virtual Task<JsonElement> GetCycleAsync(int level, int cycle)
            => Node.GetAsync($"chains/main/blocks/{level}/context/raw/json/cycle/{cycle}");

        public virtual Task<JsonElement> GetDelegateParticipationAsync(int level, string address)
            => throw new InvalidOperationException();

        public virtual Task<JsonElement> GetTicketBalance(int level, string address, string ticket)
            => throw new InvalidOperationException();

        public virtual Task<JsonElement> GetCurrentStakingBalance(int level, string address)
            => throw new InvalidOperationException();

        public virtual Task<JsonElement> GetStakingParameters(int level, string address)
            => throw new InvalidOperationException();
        #endregion
    }
}
