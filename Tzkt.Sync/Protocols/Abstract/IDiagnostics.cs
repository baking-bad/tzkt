using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols
{
    public interface IDiagnostics
    {
        Task Run(int level);
        Task Run(int level, int operations);
    }
}
