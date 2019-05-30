using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Models;

namespace Tezzycat.Services.Protocols
{
    public class ProtoInitHandler : IProtocolHandler
    {
        public string Kind => "Initialization";

        public Task<AppState> ApplyBlock(AppDbContext db, JObject block)
        {
            throw new NotImplementedException();
        }

        public Task<AppState> RevertLastBlock(AppDbContext db)
        {
            throw new NotImplementedException();
        }
    }
}
