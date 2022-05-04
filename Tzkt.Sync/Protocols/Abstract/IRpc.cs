using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IRpc
    {
        #region indexer
        Task<JsonElement> GetBlockAsync(int level);
        Task<JsonElement> GetBakingRightsAsync(int block, int cycle);
        Task<JsonElement> GetEndorsingRightsAsync(int block, int cycle);
        Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxRound);
        Task<JsonElement> GetLevelEndorsingRightsAsync(int block, int level);
        Task<JsonElement> GetContractAsync(int level, string address);
        Task<JsonElement> GetDelegateAsync(int level, string address);
        Task<JsonElement> GetStakeDistribution(int block, int cycle);
        #endregion
        
        #region diagnostics
        Task<JsonElement> GetGlobalCounterAsync(int level);
        Task<JsonElement> GetDelegatesAsync(int level);
        Task<JsonElement> GetActiveDelegatesAsync(int level);
        Task<JsonElement> GetDelegateParticipationAsync(int level, string address);
        Task<JsonElement> GetCycleAsync(int level, int cycle);
        #endregion
    }
}
