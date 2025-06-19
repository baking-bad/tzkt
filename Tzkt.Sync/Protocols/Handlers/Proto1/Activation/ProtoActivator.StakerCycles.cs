using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        protected virtual void BootstrapStakerCycles(Protocol protocol, List<Account> accounts)
        {
            // staker cycles start from proto19
        }

        protected async Task ClearStakerCycles()
        {
            await Db.Database.ExecuteSqlRawAsync("""
                DELETE FROM "StakerCycles"
                """);
        }
    }
}
