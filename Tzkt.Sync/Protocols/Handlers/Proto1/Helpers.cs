using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto1
{
    public class Helpers(ProtocolHandler proto) : IHelpers
    {
        protected CacheService Cache { get; } = proto.Cache;
        protected BlockContext Context => proto.Context;
        
        public virtual long BakingPower(Data.Models.Delegate baker)
        {
            if (!baker.Staked)
                return 0;

            var stake = baker.OwnDelegatedBalance + baker.ExternalDelegatedBalance;
            if (stake < Context.Protocol.MinimalStake)
                return 0;

            return stake - stake % Context.Protocol.MinimalStake;
        }

        public virtual long VotingPower(Data.Models.Delegate baker)
        {
            if (!baker.Staked)
                return 0;

            var stake = baker.OwnDelegatedBalance + baker.ExternalDelegatedBalance;
            if (stake < Context.Protocol.MinimalStake)
                return 0;

            return stake - stake % Context.Protocol.MinimalStake;
        }
    }
}
