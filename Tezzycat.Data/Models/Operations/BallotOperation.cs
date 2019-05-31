namespace Tezzycat.Data.Models
{
    public class BallotOperation : ProposalOperation
    {
        public Vote Vote { get; set; }
    }

    public enum Vote
    {
        Yay,
        Nay,
        Pass
    }
}
