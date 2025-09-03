using System.Collections.Generic;
using System.Linq;

namespace Modio.Extensions
{
    public static class LongExtensions
    {
        public static long RoundTimestampToHour(this long timeStamp)
        {
            // Because we dividing longs, the remainder is not kept, thusly rounding this out to hour
            var rounded = 3600 * (timeStamp / 3600);
                    
            return rounded;
        }

        public static ICollection<long> RoundTimestampsToHour(this ICollection<long> timeStamps)
            => timeStamps.Select(timeStamp => timeStamp.RoundTimestampToHour()).ToList();
    }
}
