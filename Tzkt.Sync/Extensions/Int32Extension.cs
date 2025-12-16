namespace Tzkt.Sync
{
    static class Int32Extension
    {
        public static long MulRatio(this int value, long numerator, long denominator)
        {
            return (long)(long.BigMul(value, numerator) / denominator);
        }
    }
}
