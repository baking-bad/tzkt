namespace Mvkt.Sync.Protocols
{
    static class Chains
    {
        public static string GetName(string chainId) => chainId switch
        {
            "NetXXAAR1wWQhhe" => "mainnet",
            "NetXmtMsNf69w1w" => "basenet",
            "NetXUrNc8uioxP8" => "atlasnet",
            "NetXi75cGgZdsGN" => "dailynet",
            "NetXRp4kyGKJTuB" => "weeklynet",
            _ => "private"
        };
    }
}
