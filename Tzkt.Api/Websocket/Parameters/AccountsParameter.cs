using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class AccountsParameter
    {
        public string Address { get; set; }

        public void EnsureValid()
        {
            if (Address != null && !Regex.IsMatch(Address, "^(tz1|tz2|tz3|KT1)[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");
        }
    }
}