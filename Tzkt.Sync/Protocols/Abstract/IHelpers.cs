namespace Tzkt.Sync.Protocols
{
    public interface IHelpers
    {
        long BakingPower(Data.Models.Delegate baker);
        long VotingPower(Data.Models.Delegate baker);
    }
}
