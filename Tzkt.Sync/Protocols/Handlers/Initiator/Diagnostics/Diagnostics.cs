using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols.Initiator
{
    class Diagnostics : IDiagnostics
    {
        public Task Run(int level) => Task.CompletedTask;
        public Task Run(int level, int operations) => Task.CompletedTask;
    }
}
