namespace Tzkt.Api
{
    static class EpochStatuses
    {
        public const string NoProposals = "no_proposals";
        public const string Voting = "voting";
        public const string Completed = "completed";
        public const string Failed = "failed";

        public static bool IsValid(string value) => value switch
        {
            NoProposals => true,
            Voting => true,
            Completed => true,
            Failed => true,
            _ => false
        };
    }
}
