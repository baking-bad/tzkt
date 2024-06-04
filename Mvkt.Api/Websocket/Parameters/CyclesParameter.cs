using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using Mvkt.Api.Models;

namespace Mvkt.Api.Websocket
{
    public class CyclesParameter
    {
        public int DelayBlocks { get; set; } = 2; // 2 to cover possible reorganization
        
        public void EnsureValid()
        {
            if (DelayBlocks < 2)
                throw new HubException("DelayBlocks has to greater than or equal to 2.");
        }
    }
}
