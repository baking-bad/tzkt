using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync
{
    public interface IProtocolHandler
    {
        string Protocol { get; }

        Task<AppState> ApplyBlock(JObject block);
        Task<AppState> RevertLastBlock();
    }
}
