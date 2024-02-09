using System.Numerics;

namespace Mvkt.Sync.Protocols
{
    public class TicketUpdates
    {
        public TicketIdentity Ticket { get; set; }
        public IEnumerable<TicketUpdate> Updates { get; set; }
    }
    
    public class TicketIdentity
    {
        public string Ticketer { get; set; }
        public byte[] RawType { get; set; }
        public byte[] RawContent { get; set; }
        public string JsonContent { get; set; }

        public int ContentHash { get; set; }
        public int TypeHash { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TicketIdentity ticket &&
                ticket.Ticketer == Ticketer &&
                ticket.RawType.IsEqual(RawType) &&
                ticket.RawContent.IsEqual(RawContent);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ticketer.GetHashCode(), ContentHash, TypeHash);
        }
    }
    
    public class TicketUpdate
    {
        public string Account { get; set; }
        public BigInteger Amount { get; set; }
    }
}