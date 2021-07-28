using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace Tzkt.Api.Websocket
{
    public class AccountsParameter
    {
        public List<string> Addresses { get; set; }

        public void EnsureValid()
        {
            if (Addresses.Any(string.IsNullOrEmpty))
                throw new HubException("Empty address parameter");
            if (Addresses.Any(x => !Regex.IsMatch(x, "^(tz1|tz2|tz3|KT1)[0-9A-Za-z]{33}$")))
                throw new HubException("Invalid subscription address");
        }
    }
}