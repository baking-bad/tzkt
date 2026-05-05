using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    public interface IActivator
    {
        Task ActivateContext(AppState state, JsonElement block);

        Task DeactivateContext(AppState state);
    }
}
