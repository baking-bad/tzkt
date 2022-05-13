using System.Linq;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class VotingCommit : Proto1.VotingCommit
    {
        public VotingCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override int GetParticipationEma(VotingPeriod period, Protocol proto)
        {
            var prev = Db.VotingPeriods
                .AsNoTracking()
                .OrderByDescending(x => x.Index)
                .FirstOrDefault(x => x.Kind == PeriodKind.Exploration || x.Kind == PeriodKind.Promotion);

            if (prev != null)
            {
                var participation = 10000 * (prev.YayVotingPower + prev.NayVotingPower + prev.PassVotingPower) / prev.TotalVotingPower;
                return ((int)prev.ParticipationEma * 8000 + (int)participation * 2000) / 10000;
            }

            return proto.BallotQuorumMax;
        }

        protected override int GetBallotQuorum(VotingPeriod period, Protocol proto)
        {
            return proto.BallotQuorumMin + (int)period.ParticipationEma * (proto.BallotQuorumMax - proto.BallotQuorumMin) / 10000;
        }
    }
}
