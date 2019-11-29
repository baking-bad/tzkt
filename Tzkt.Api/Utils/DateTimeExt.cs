using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api
{
    public static class DateTimeExt
    {
        public static DateTime Max(DateTime dt1, DateTime dt2) => dt1 > dt2 ? dt1 : dt2;
        public static DateTime Min(DateTime dt1, DateTime dt2) => dt1 > dt2 ? dt2 : dt1;
    }
}
