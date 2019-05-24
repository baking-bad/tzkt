using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tezzycat.Models;

namespace Tezzycat.Services
{
    public interface IProtocolHandler
    {
        string Kind { get; }

        Task<AppState> ApplyBlock(JObject block);
        Task<AppState> RevertLastBlock();
    }
}
