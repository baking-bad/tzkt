using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Data.Models;

namespace Tezzycat.Sync.Services.Protocols
{
    public class MigrationHandler : IProtocolHandler
    {
        public string Kind => "InitProto";

        public Task<AppState> ApplyBlock(SyncContext db, JObject block)
        {
            throw new NotImplementedException();
        }

        public Task<AppState> RevertLastBlock(SyncContext db)
        {
            throw new NotImplementedException();
        }
    }
}
