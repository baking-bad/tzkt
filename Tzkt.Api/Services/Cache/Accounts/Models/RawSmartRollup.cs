namespace Tzkt.Api.Services.Cache
{
    public class RawSmartRollup : RawAccount
    {
        public int CreatorId { get; set; }
        public int PvmKind { get; set; }
        public required string GenesisCommitment { get; set; }
        public required string LastCommitment { get; set; }
        public int InboxLevel { get; set; }
        public int TotalStakers { get; set; }
        public int ActiveStakers { get; set; }
        public int ExecutedCommitments { get; set; }
        public int CementedCommitments { get; set; }
        public int PendingCommitments { get; set; }
        public int RefutedCommitments { get; set; }
        public int OrphanCommitments { get; set; }
    }
}
