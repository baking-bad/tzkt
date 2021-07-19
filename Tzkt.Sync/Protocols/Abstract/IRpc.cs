using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IRpc
    {
        #region indexer
        Task<JsonElement> GetBlockAsync(int level);
        Task<JsonElement> GetCycleAsync(int level, int cycle);
        Task<JsonElement> GetBakingRightsAsync(int block, int cycle);
        Task<JsonElement> GetEndorsingRightsAsync(int block, int cycle);
        Task<JsonElement> GetLevelBakingRightsAsync(int block, int level, int maxPriority);
        Task<JsonElement> GetLevelEndorsingRightsAsync(int block, int level);
        Task<JsonElement> GetContractAsync(int level, string address);
        Task<JsonElement> GetDelegateAsync(int level, string address);
        #endregion
        
        #region diagnostics
        Task<JsonElement> GetGlobalCounterAsync(int level);
        #endregion
    }
}
