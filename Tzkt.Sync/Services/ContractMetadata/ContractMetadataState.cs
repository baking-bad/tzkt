namespace Tzkt.Sync.Services
{
    public class ContractMetadataState
    {
        public Dictionary<string, ContractMetadataDipDupState> DipDup { get; set; } = [];
    }

    public class ContractMetadataDipDupState
    {
        public long LastUpdateId { get; set; } = 0;
        public string Sentinel { get; set; } = string.Empty;
    }
}
