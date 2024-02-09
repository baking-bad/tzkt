using System.Text.Json;
using System.Threading.Tasks;

namespace Mvkt.Sync.Protocols
{
    public interface IValidator
    {
        Task ValidateBlock(JsonElement block);
    }
}
