namespace Tzkt.Sync
{
    static class DateTimeExtension
    {
        public static DateTime TrimMilliseconds(this DateTime value)
        {
            return value.AddTicks(-(value.Ticks % 10_000_000)); ;
        }
    }
}
