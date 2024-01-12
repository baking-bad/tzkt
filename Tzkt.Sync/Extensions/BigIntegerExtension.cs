using System.Numerics;

namespace Tzkt.Sync
{
    static class BigIntegerExtension
    {
        public static long TrimToInt64(this BigInteger value)
        {
            return value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;
        }
    }
}
