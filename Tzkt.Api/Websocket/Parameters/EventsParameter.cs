using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class EventsParameter
    {
        public int? CodeHash { get; set; }
        public string Contract { get; set; }
        public string Tag { get; set; }

        public void EnsureValid()
        {
            if (Contract != null && !Regex.IsMatch(Contract, "^KT1[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");

            if (Tag != null && Tag.Length > 256)
                throw new HubException("Too long tag");
        }
    }
}