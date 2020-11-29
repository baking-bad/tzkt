using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IRpc
    {
        #region indexer
        Task<JsonElement> GetBlockAsync(int level);
        Task<JsonElement> GetCycleAsync(int level, int cycle);
        Task<JsonElement> GetBakingRightsAsync(int level, int cycle);
        Task<JsonElement> GetEndorsingRightsAsync(int level, int cycle);
        Task<JsonElement> GetLevelBakingRightsAsync(int level, int maxPriority);
        #endregion

        #region bootstrap
        Task<JsonElement> GetAllContractsAsync(int level);
        #endregion
        
        #region diagnostics
        Task<JsonElement> GetGlobalCounterAsync(int level);
        Task<JsonElement> GetContractAsync(int level, string address);
        Task<JsonElement> GetDelegateAsync(int level, string address);
        #endregion
    }
}
