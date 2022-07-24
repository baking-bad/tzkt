using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using Tzkt.Api.Models;

namespace Tzkt.Api.Websocket
{
    public class CyclesParameter
    {
        public int DelayBlocks { get; set; } = 2; // 2 to cover possible reorganization
        public Symbols Quote { get; set; } = Symbols.None;

        public void EnsureValid()
        {
            if (DelayBlocks < 2)
                throw new HubException("DelayBlocks has to greater than or equal to 2.");
        }
    }
}
