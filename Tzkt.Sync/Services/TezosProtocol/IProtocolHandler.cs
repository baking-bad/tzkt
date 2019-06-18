using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public interface IProtocolHandler
    {
        string Kind { get; }

        Task<AppState> ApplyBlock(JObject block);
        Task<AppState> RevertLastBlock();
    }
}
