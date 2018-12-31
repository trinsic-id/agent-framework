using System;

namespace AgentFramework.Core.Extensions
{
    public static class DatetimeExtensions
    {
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static long ToUnixTimeMilliseconds(this DateTime datetime) => ((DateTimeOffset) datetime).ToUnixTimeMilliseconds();

        public static long ToUnixTimeMilliseconds(this DateTime? datetime) => ((DateTimeOffset)datetime.Value).ToUnixTimeMilliseconds();

        public static DateTime FromUnixTimeMilliseconds(long ticks) => DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeMilliseconds(ticks).DateTime, DateTimeKind.Utc);
    }
}
