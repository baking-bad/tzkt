namespace Mvkt.Sync.Protocols
{
    static class Chains
    {
        public static string GetName(string chainId) => chainId switch
        {
            "NetXdQprcVkpaWU" => "mainnet",
            "NetXUrNc8uioxP8" => "basenet",
            "NetXUrNc8uioxP8" => "atlasnet",
            "NetXUrNc8uioxP8" => "boreasnet",
            _ => "private"
        };
    }
}
