using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Tzkt.Data.Models;

namespace Tzkt.Api.Websocket
{
    public class BigMapsParameter
    {
        public int? Ptr { get; set; }
        public string Path { get; set; }
        public string Contract { get; set; }
        public List<string> Tags { get; set; }

        List<BigMapTag> _TagsList = null;
        public List<BigMapTag> TagsList
        {
            get
            {
                if (_TagsList == null && Tags != null)
                {
                    _TagsList = new(Tags.Count);
                    foreach (var tag in Tags)
                    {
                        if (!BigMapTags.TryParse(tag, out var res))
                            throw new HubException("Invalid bigmap tag");
                        _TagsList.Add((BigMapTag)res);
                    }
                }
                return _TagsList;
            }
        }

        public void EnsureValid()
        {
            if (Ptr != null && Ptr < 0)
                throw new HubException("Invalid ptr");
            
            if (Contract != null && !Regex.IsMatch(Contract, "^KT1[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid contract address");

            if (Path != null && Path.Length > 256)
                throw new HubException("Too long path");

            if (TagsList?.Count == 0)
                throw new HubException("Invalid tags");
        }
    }
}