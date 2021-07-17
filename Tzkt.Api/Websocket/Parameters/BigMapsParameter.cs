using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tzkt.Api.Websocket
{
    public class BigMapsParameter
    {
        public int? Ptr { get; set; }
        public string Path { get; set; }
        public string Contract { get; set; }
        public List<string> Tags { get; set; }

        public void EnsureValid()
        {
            if (Ptr != null && Ptr < 0)
                throw new HubException("Invalid ptr");
            
            if (Contract != null && !Regex.IsMatch(Contract, "^KT1[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");

            if (Path != null && Path.Length > 256)
                throw new HubException("Too long path");

            if (Tags != null && Tags.All(x => BigMapTags.IsValid(x)))
                throw new HubException("Invalid tags");
        }
    }
}