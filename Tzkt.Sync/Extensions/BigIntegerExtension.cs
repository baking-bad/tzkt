using System.Numerics;

namespace Tzkt.Sync
{
    static class BigIntegerExtension
    {
        public static long TrimToInt64(this BigInteger value)
        {
            return value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;
        }

        public static bool Mem(this BigInteger number, int bitPosition)
        {
            if (bitPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition), "Bit position must be non-negative.");
            }

            BigInteger mask = BigInteger.One << bitPosition;
            return (number & mask) != 0;
        }
    }
}
