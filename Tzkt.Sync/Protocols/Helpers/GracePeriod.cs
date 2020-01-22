using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class GracePeriod
    {
        public static int Init(Block block)
            => Init(block.Level, block.Protocol.BlocksPerCycle, block.Protocol.PreservedCycles);

        public static int Init(int level, int cycleLength, int preserved)
            => ((level - 1) / cycleLength + preserved * 2 + 2) * cycleLength + 1;
         
        public static int Reset(Block block)
            => Reset(block.Level, block.Protocol.BlocksPerCycle, block.Protocol.PreservedCycles);

        public static int Reset(int level, int cycleLength, int preserved)
            => ((level - 1) / cycleLength + preserved + 2) * cycleLength + 1;
    }
}
