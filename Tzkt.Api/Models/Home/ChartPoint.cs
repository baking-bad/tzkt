using System;

namespace Tzkt.Api.Models
{
    public class ChartPoint : ChartPoint<long> { }

    public class ChartPoint<T>
    {
        public DateTime Date { get; set; }
        public T Value { get; set; }
    }
}