using System.Numerics;

namespace Tzkt.Sync.Protocols
{
    public class TicketUpdates
    {
        public required TicketIdentity Ticket { get; set; }
        public required IEnumerable<TicketUpdate> Updates { get; set; }
    }
    
    public class TicketIdentity
    {
        public required string Ticketer { get; set; }
        public required byte[] RawType { get; set; }
        public required byte[] RawContent { get; set; }
        public string? JsonContent { get; set; }

        public int ContentHash { get; set; }
        public int TypeHash { get; set; }

        public override bool Equals(object? obj)
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
        public required string Account { get; set; }
        public BigInteger Amount { get; set; }
    }
}