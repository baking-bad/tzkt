using System.Text.Json;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Rpc : IRpc
    {
        readonly MavrykNode Node;

        public Rpc(MavrykNode node) => Node = node;

        #region indexer
        public Task<JsonElement> GetBlockAsync(int level)
            => Node.GetAsync($"chains/main/blocks/{level}");

        public Task<JsonElement> GetBakingRightsAsync(int block, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetEndorsingRightsAsync(int block, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxRound)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetLevelEndorsingRightsAsync(int block, int level)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetContractAsync(int level, string address)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetDelegateAsync(int level, string address)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetStakeDistribution(int block, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetExpectedIssuance(int level)
            => throw new InvalidOperationException();
        #endregion

        #region diagnostics
        public Task<JsonElement> GetGlobalCounterAsync(int level)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetDelegatesAsync(int level)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetActiveDelegatesAsync(int level)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetCycleAsync(int level, int cycle)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetDelegateParticipationAsync(int level, string address)
            => throw new InvalidOperationException();
        
        public Task<JsonElement> GetTicketBalance(int level, string address, string ticket)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetCurrentStakingBalance(int level, string address)
            => throw new InvalidOperationException();

        public Task<JsonElement> GetStakingParameters(int level, string address)
            => throw new InvalidOperationException();
        #endregion
    }
}
