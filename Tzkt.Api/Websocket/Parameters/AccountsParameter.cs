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
            if (Addresses?.Count > 0)
            {
                if (Addresses.Any(string.IsNullOrEmpty))
                    throw new HubException("Empty address. Array should not contain nulls or empty strings");
                if (Addresses.Any(x => !Regex.IsMatch(x, "^(tz1|tz2|tz3|KT1|txr1)[0-9A-Za-z]{33}$")))
                    throw new HubException("Array contains an invalid address");
            }
        }
    }
}