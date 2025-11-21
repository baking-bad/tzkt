namespace Tzkt.Sync.Protocols
{
    static class Chains
    {
        public static string GetName(string chainId) => chainId switch
        {
            "NetXdQprcVkpaWU" => "mainnet",
            "NetXSgo1ZT2DRUG" => "edo2net",
            "NetXxkAx4woPLyu" => "florencenet",
            "NetXz969SFaFn8k" => "granadanet",
            "NetXuXoGoLxNK6o" => "hangzhounet",
            "NetXZSsxBpMQeAT" => "hangzhou2net",
            "NetXLH1uAxK7CCh" => "jakartanet",
            "NetXnHfVqm9iesp" => "ghostnet",
            "NetXi2ZagzEsXbZ" => "kathmandunet",
            "NetXizpkH94bocH" => "limanet",
            "NetXgbcrNtXD2yA" => "mumbainet",
            "NetXyuzvDo2Ugzb" => "nairobinet",
            "NetXxWsskGahzQB" => "oxfordnet",
            "NetXXWAHLEvre9b" => "parisnet",
            "NetXuTeGinLEqxp" => "quebecnet",
            "NetXPdgaoabtBth" => "rionet",
            "NetXd56aBs1aeW3" => "seoulnet",
            "NetXe8DbhW9A1eS" => "tallinnnet",
            "NetXYRoAfQDxZux" => "nextnet",
            _ => "private"
        };
    }
}
