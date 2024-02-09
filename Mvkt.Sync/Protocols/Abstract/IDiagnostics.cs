using System.Text.Json;

namespace Mvkt.Sync.Protocols
{
    public interface IDiagnostics
    {
        void TrackChanges();
        Task Run(JsonElement block);
        Task Run(int level);
    }
}
