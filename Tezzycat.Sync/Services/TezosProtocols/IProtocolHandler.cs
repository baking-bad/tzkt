using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Data.Models;

namespace Tezzycat.Sync.Services
{
    public interface IProtocolHandler
    {
        string Kind { get; }

        Task<AppState> ApplyBlock(SyncContext db, JObject block);
        Task<AppState> RevertLastBlock(SyncContext db);
    }
}
