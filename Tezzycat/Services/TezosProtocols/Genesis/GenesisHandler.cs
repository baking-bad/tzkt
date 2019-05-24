using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tezzycat.Models;

namespace Tezzycat.Services.Protocols
{
    public class GenesisHandler : IProtocolHandler
    {
        public string Kind => "Genesis";

        public Task<AppState> ApplyBlock(JObject block)
        {
            throw new NotImplementedException();
        }

        public Task<AppState> RevertLastBlock()
        {
            throw new NotImplementedException();
        }
    }
}
