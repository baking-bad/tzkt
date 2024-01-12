﻿using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols.Genesis
{
    class Diagnostics : IDiagnostics
    {
        public void TrackChanges() { }
        public Task Run(JsonElement block) => Task.CompletedTask;
        public Task Run(int level) => Task.CompletedTask;
    }
}
