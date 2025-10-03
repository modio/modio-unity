using System;

namespace Modio.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetUtcDateTime(this long timeStamp)
        {
            DateTime dateTime = DateTime.UnixEpoch.AddSeconds(timeStamp);
            return dateTime;
        }
        public static DateTime GetLocalDateTime(this long timeStamp)
        {
            DateTime dateTime = DateTime.UnixEpoch.AddSeconds(timeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
