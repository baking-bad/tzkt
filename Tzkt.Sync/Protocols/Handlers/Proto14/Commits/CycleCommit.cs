using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto14
{
    class CycleCommit : Proto13.CycleCommit
    {
        public CycleCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override async Task<byte[]> GetVdfSolution(Block block)
        {
            return (await Db.VdfRevelationOps
                .AsNoTracking()
                .Where(x => x.Cycle == block.Cycle - 1)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync())?.Solution;
        }
    }
}
