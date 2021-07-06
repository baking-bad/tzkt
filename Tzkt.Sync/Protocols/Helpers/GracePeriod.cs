using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class GracePeriod
    {
        public static int Init(Block block)
            => Init(block.Level, block.Protocol);

        public static int Init(int level, Protocol proto)
            => proto.GetCycleStart(proto.GetCycle(level) + proto.PreservedCycles * 2 + 2);

        public static int Reset(Block block)
            => Reset(block.Level, block.Protocol);

        public static int Reset(int level, Protocol proto)
            => proto.GetCycleStart(proto.GetCycle(level) + proto.PreservedCycles + 2);
    }
}
