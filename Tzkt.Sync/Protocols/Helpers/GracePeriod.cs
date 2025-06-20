﻿using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols
{
    static class GracePeriod
    {
        public static int Init(int level, Protocol proto)
            => proto.GetCycleStart(proto.GetCycle(level) + proto.ConsensusRightsDelay + proto.ToleratedInactivityPeriod + 1);

        public static int Reset(int level, Protocol proto)
            => proto.GetCycleStart(proto.GetCycle(level) + proto.ToleratedInactivityPeriod + 1);
    }
}
