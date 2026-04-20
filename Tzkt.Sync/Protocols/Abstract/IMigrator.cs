using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    public interface IMigrator
    {
        Task MigrateContext(AppState state);

        Task RevertContext(AppState state);
    }
}
