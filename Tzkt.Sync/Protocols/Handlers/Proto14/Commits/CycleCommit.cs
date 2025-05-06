using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    class CycleCommit(ProtocolHandler protocol) : Proto13.CycleCommit(protocol)
    {
        protected override async Task<byte[]?> GetVdfSolution(Block block)
        {
            return (await Db.VdfRevelationOps
                .AsNoTracking()
                .Where(x => x.Cycle == block.Cycle - 1)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync())?.Solution;
        }
    }
}
