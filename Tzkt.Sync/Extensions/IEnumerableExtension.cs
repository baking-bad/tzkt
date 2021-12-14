using System;
using System.Collections.Generic;
using System.Numerics;

namespace Tzkt.Sync
{
    static class IEnumerableExtension
    {
        public static BigInteger BigSum<T>(this IEnumerable<T> items, Func<T, BigInteger> selector)
        {
            var res = BigInteger.Zero;
            foreach (var item in items) res += selector(item);
            return res;
        }
    }
}
