using Netezos.Encoding;

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
        public IMicheline ContentType { get; set; }
        public IMicheline Content { get; set; }
    }
    
    public class Update
    {
        public string Account { get; set; }
        public string Amount { get; set; }
    }
}