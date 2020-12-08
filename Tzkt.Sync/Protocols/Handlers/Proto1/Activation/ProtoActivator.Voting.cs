using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto1
{
    partial class ProtoActivator : ProtocolCommit
    {
        public void BootstrapVoting(Protocol protocol)
        {
            var period = new ProposalPeriod
            {
                Code = 0,
                Epoch = new VotingEpoch { Level = 1 },
                Kind = VotingPeriods.Proposal,
                StartLevel = 1,
                EndLevel = protocol.BlocksPerVoting
            };

            Db.VotingPeriods.Add(period);
            Cache.Periods.Add(period);
        }

        public async Task ClearVoting()
        {
            await Db.Database.ExecuteSqlRawAsync($@"
                DELETE FROM ""VotingEpoches"";
                DELETE FROM ""VotingPeriods"";");
            Cache.Periods.Reset();
        }
    }
}
