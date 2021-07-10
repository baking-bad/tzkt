using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class AccountsParameter
    {
        public string Address { get; set; }

        public void EnsureValid()
        {
            if (Address != null && !Regex.IsMatch(Address, @"(^tz[0-9A-z]{34}$)|(^KT[0-9A-z]{34}$)"))
                throw new HubException("Invalid contract address");
        }
    }
}