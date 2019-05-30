using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Models;

namespace Tezzycat.Services
{
    public interface IProtocolHandler
    {
        string Kind { get; }

        Task<AppState> ApplyBlock(AppDbContext db, JObject block);
        Task<AppState> RevertLastBlock(AppDbContext db);
    }
}
