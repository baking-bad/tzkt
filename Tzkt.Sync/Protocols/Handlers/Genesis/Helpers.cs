namespace Tzkt.Sync.Protocols.Genesis
{
    public class Helpers() : IHelpers
    {
        public virtual long BakingPower(Data.Models.Delegate baker)
            => throw new NotImplementedException();

        public virtual long VotingPower(Data.Models.Delegate baker)
            => throw new NotImplementedException();
    }
}
