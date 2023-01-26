namespace Tzkt.Sync.Services
{
    public class ContractMetadataConfig
    {
        public bool Enabled { get; set; } = false;       
        public int Period { get; set; } = 60;
        public List<DipDupConfig> DipDup { get; set; }
    }

    public static class ContractMetadataConfigExt
    {
        public static ContractMetadataConfig GetContractMetadataConfig(this IConfiguration config)
        {
            return config.GetSection("ContractMetadata")?.Get<ContractMetadataConfig>() ?? new();
        }
    }
}
