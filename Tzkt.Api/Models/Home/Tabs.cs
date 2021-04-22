using System.Collections.Generic;

namespace Tzkt.Api.Models.Home
{
    public class Tabs
    {
        //TODO Oncoming blocks
        public object[][] Blocks { get; set; }
        public object[][] Accounts { get; set; }
        public object[][] Assets { get; set; }
        public IEnumerable<Delegate> Bakers { get; set; }
    }
    
    public enum TabTypes
    {
        Blocks,
        Accounts,
        Bakers,
        Assets
    }
}