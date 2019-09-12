using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tzkt.Data;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    public class Proto3Handler : IProtocolHandler
    {
        public string Protocol => "Proto3";

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
