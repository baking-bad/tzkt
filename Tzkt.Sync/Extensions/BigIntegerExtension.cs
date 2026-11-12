using System.Numerics;

namespace Tzkt.Sync
{
    static class BigIntegerExtension
    {
        public static int TrimToInt32(this BigInteger value)
        {
            return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
        }

        public static long TrimToInt64(this BigInteger value)
        {
            return value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;
        }
    }
}
