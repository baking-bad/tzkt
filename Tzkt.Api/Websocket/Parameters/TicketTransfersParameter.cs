using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class TicketTransfersParameter
    {
        public string Account { get; set; }
        public string Ticketer { get; set; }
        public long? TicketId { get; set; }

        public void EnsureValid()
        {
            if (Account != null && !Regex.IsMatch(Account, "^[0-9A-Za-z]{36,37}$"))
                throw new HubException("Invalid account address");

            if (Ticketer != null && !Regex.IsMatch(Ticketer, "^KT1[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");
            
            if (TicketId is < 0)
                throw new HubException("Invalid ticketId");
        }
    }
}