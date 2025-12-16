using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class VotingCommit(ProtocolHandler protocol) : Proto1.VotingCommit(protocol)
    {
        protected override int GetParticipationEma(VotingPeriod period, Protocol proto)
        {
            var prev = Db.VotingPeriods
                .AsNoTracking()
                .OrderByDescending(x => x.Index)
                .FirstOrDefault(x => x.Kind == PeriodKind.Exploration || x.Kind == PeriodKind.Promotion);

            if (prev != null)
            {
                var participation = 10000.MulRatio(prev.YayVotingPower!.Value + prev.NayVotingPower!.Value + prev.PassVotingPower!.Value, prev.TotalVotingPower);
                return (int)((prev.ParticipationEma!.Value * 8000 + participation * 2000) / 10000);
            }

            return proto.BallotQuorumMax;
        }

        protected override int GetBallotQuorum(VotingPeriod period, Protocol proto)
        {
            return proto.BallotQuorumMin + period.ParticipationEma!.Value * (proto.BallotQuorumMax - proto.BallotQuorumMin) / 10000;
        }
    }
}
