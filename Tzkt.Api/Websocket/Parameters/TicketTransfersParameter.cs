using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class TicketTransfersParameter
    {
        public string? Account { get; set; }
        public string? Ticketer { get; set; }

        public void EnsureValid()
        {
            if (Account != null && !Regexes.Address().IsMatch(Account))
                throw new HubException("Invalid account address");

            if (Ticketer != null && !Regexes.Kt1Address().IsMatch(Ticketer))
                throw new HubException("Invalid contract address");
        }
    }
}