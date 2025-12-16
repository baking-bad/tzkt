namespace Tzkt.Sync
{
    static class Int64Extension
    {
        public static long MulRatio(this long value, int numerator, int denominator)
        {
            return (long)(long.BigMul(value, numerator) / denominator);
        }

        public static long MulRatio(this long value, long numerator, long denominator)
        {
            return (long)(long.BigMul(value, numerator) / denominator);
        }

        public static long MulRatioUp(this long value, long numerator, long denominator)
        {
            return (long)((long.BigMul(value, numerator) + (denominator - 1)) / denominator);
        }
    }
}
