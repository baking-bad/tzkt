using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IDiagnostics
    {
        Task Run(JsonElement block);
        Task Run(int level);
    }
}
