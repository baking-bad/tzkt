using System.Text.Json;

namespace Tzkt.Sync.Protocols
{
    public interface IRpc
    {
        #region indexer
        Task<JsonElement> GetBlockAsync(int level);
        Task<JsonElement> GetBakingRightsAsync(int block, int cycle);
        Task<JsonElement> GetAttestationRightsAsync(int block, int cycle);
        Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxRound);
        Task<JsonElement> GetLevelAttestationRightsAsync(int block, int level);
        Task<JsonElement> GetContractAsync(int level, string address);
        Task<JsonElement> GetDelegateAsync(int level, string address);
        Task<JsonElement> GetStakeDistribution(int block, int cycle);
        Task<JsonElement> GetExpectedIssuance(int level);
        Task<JsonElement> GetSmartRollupGenesisInfo(int level, string address);
        Task<JsonElement> GetUnstakeRequests(int level, string address);
        Task<JsonElement> GetContractRawAsync(int level, string address);
        #endregion

        #region diagnostics
        Task<JsonElement> GetGlobalCounterAsync(int level);
        Task<JsonElement> GetDelegatesAsync(int level);
        Task<JsonElement> GetActiveDelegatesAsync(int level);
        Task<JsonElement> GetDelegateParticipationAsync(int level, string address);
        Task<JsonElement> GetDelegateDalParticipationAsync(int level, string address);
        Task<JsonElement> GetCycleAsync(int level, int cycle);
        Task<JsonElement> GetTicketBalance(int level, string address, string ticket);
        Task<JsonElement> GetCurrentStakingBalance(int level, string address);
        Task<JsonElement> GetStakingParameters(int level, string address);
        #endregion
    }
}
