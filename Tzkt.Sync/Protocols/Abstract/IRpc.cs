using System.Text.Json;

namespace Tzkt.Sync.Protocols
{
    public interface IRpc
    {
        #region indexer
        Task<JsonElement> GetBlockAsync(int level);
        Task<JsonElement> GetContractAsync(int level, string address);
        Task<JsonElement> GetContractManagerKeyAsync(int level, string address);
        Task<JsonElement> GetConstantsAsync(int level);
        Task<JsonElement> GetContractsAsync(int level);
        #endregion
    }
}
