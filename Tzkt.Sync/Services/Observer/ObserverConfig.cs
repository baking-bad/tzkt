namespace Tzkt.Sync.Services
{
    public class ObserverConfig
    {
        public string Method { get; set; } = "polling";
        public bool Debounce { get; set; } = true;
        public int Period { get; set; } = 1000;
        public int Lag { get; set; } = 0;
    }

    public static class ObserverConfigExt
    {
        public static ObserverConfig GetObserverConfig(this IConfiguration config)
        {
            var res = config.GetSection("Observer")?.Get<ObserverConfig>() ?? new();
            #region deprecated
            var legacyLag = config.GetValue<int>("TezosNode:Lag");
            if (res.Lag == 0 && legacyLag != 0)
                res.Lag = legacyLag;
            #endregion
            return res;
        }
    }
}
