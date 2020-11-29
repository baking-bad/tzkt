using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IValidator
    {
        Task ValidateBlock(JsonElement block);
    }
}
