using System.Text.Json;

namespace Tzkt.Sync.Protocols
{
    public interface IValidator
    {
        Task ValidateBlock(JsonElement block);
    }
}
