using System;
using System.Collections.Generic;

namespace ModIO.Util
{
    public static class EnumExtensions
    {
        public static IEnumerable<string> ExtractBitFlagsFromEnum<T>(this T value) where T : Enum
        {
            foreach(T foo in Enum.GetValues(typeof(T)))
            {
                int fooInt = Convert.ToInt32(foo);
                int valueAsInt = Convert.ToInt32(value);

                if(fooInt != 0 && (valueAsInt & fooInt) != 0)
                {
                    yield return foo.ToString();
                }
            }
        }

    }
}
