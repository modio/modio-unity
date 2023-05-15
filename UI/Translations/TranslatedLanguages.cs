#if UNITY_EDITOR
#endif

using System;
using System.Globalization;

namespace ModIOBrowser.Implementation
{
    public enum TranslatedLanguages
    {
        English = 0, //English.po
        Swedish = 1  //Swedish.po
    }

    static class TranslatedLanguagesExtensions
    {
        public static CultureInfo Culture(this TranslatedLanguages language)
        {
            switch(language)
            {
                case TranslatedLanguages.English: return CultureInfo.GetCultureInfo("en-US");
                case TranslatedLanguages.Swedish: return CultureInfo.GetCultureInfo("sv-SE");
            }

            return CultureInfo.GetCultureInfo("en-US");
        }

        public static string Date(this TranslatedLanguages language, DateTime date)
            => date.ToString(language.Culture());

        public static string DateShort(this TranslatedLanguages language, DateTime date)
            => date.ToString(language.Culture().DateTimeFormat.ShortDatePattern);

        public static string Number<T>(this TranslatedLanguages language, T number) where T : IFormattable
            => number.ToString("n", language.Culture());


    }
}
