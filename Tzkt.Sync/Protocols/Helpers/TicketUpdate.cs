using System.Numerics;

namespace Tzkt.Sync.Protocols
{
    public class TicketUpdate
    {
        public TicketToken TicketToken { get; set; }
        public IEnumerable<Update> Updates { get; set; }
    }
    
    public class TicketToken
    {
        public string Ticketer { get; set; }
        public byte[] RawContent { get; set; }
        public byte[] RawType { get; set; }
        public string JsonContent { get; set; }
        public string JsonType { get; set; }

        public int ContentHash { get; set; }
        public int TypeHash { get; set; }
    }
    
    public class Update
    {
        public string Account { get; set; }
        public BigInteger Amount { get; set; }
    }
}