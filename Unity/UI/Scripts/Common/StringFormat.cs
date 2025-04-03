using System;
using Modio.Unity.UI.Components.Localization;
using UnityEngine;

namespace Modio.Unity.UI
{
    public enum StringFormatBytes
    {
        Bytes,
        BytesComma,
        Suffix,
        Custom,
    }

    public enum StringFormatKilo
    {
        None,
        Comma,
        Kilo,
        Custom,
    }

    internal static class StringFormat
    {
#region Bytes

        public const string BYTES_FORMAT_TOOLTIP = @"Bytes: ""1048576"".
BytesComma: ""1,048,576"".
Suffix: ""1 MB"".";

        static readonly string[] BytesSuffixes = { "B", "KB", "MB", "GB" };
        static readonly string[] BytesSuffixesLoc =
        {
            "modio_unit_bytes", "modio_unit_kb", "modio_unit_mb", "modio_unit_gb"
        };

        public static string Bytes(StringFormatBytes format, long bytes, string custom = null, bool reducePrecision = false) => format switch
        {
            StringFormatBytes.Bytes      => bytes.ToString(),
            StringFormatBytes.BytesComma => BytesComma(bytes),
            StringFormatBytes.Suffix     => BytesSuffix(bytes, reducePrecision),
            StringFormatBytes.Custom => string.IsNullOrEmpty(custom)
                ? bytes.ToString(ModioUILocalizationManager.CultureInfo)
                : bytes.ToString(custom, ModioUILocalizationManager.CultureInfo),
            _ => bytes.ToString(),
        };

        public static string BytesComma(long bytes) => bytes.ToString("N0", ModioUILocalizationManager.CultureInfo);

        public static string BytesSuffix(long bytes, bool reducePrecision = false)
        {
            const int minSizeToDisplay =
                1; // ignore bytes, skip to KB. It's clearer in many cases and avoids 128.00 which looks odd (fractional bytes? what?)

            int index = Mathf.Clamp((int)Math.Log(bytes, 1024), minSizeToDisplay, BytesSuffixes.Length - 1);

            double size = bytes / Math.Pow(1024, index);
            var bytesSuffix = BytesSuffixes[index];
            bytesSuffix = ModioUILocalizationManager.GetLocalizedText(BytesSuffixesLoc[index], false) ?? bytesSuffix;

            var precision = reducePrecision ? Mathf.Max(0, 2 - (int)Mathf.Log10(bytes)) : 2;

            return $"{size.ToString($"F{precision}")} {bytesSuffix}";
        }

#endregion

#region Kilo

        public const string KILO_FORMAT_TOOLTIP = @"None: ""10500"".
Comma: ""10,500"".
Kilo: ""10.5k"".";

        public static string Kilo(StringFormatKilo format, long value, string custom = null) => format switch
        {
            StringFormatKilo.None   => value.ToString(),
            StringFormatKilo.Comma  => value.ToString("N0"),
            StringFormatKilo.Kilo   => Kilo(value),
            StringFormatKilo.Custom => string.IsNullOrEmpty(custom) ? value.ToString() : value.ToString(custom),
            _                       => value.ToString(),
        };

        public static string Kilo(long value)
        {
            if (value > 1000000000000) return (value / 1000000000000D).ToString("0.#T");
            if (value > 100000000000) return (value / 1000000000000D).ToString("0.##T");
            if (value > 10000000000) return (value / 1000000000D).ToString("0.#G");
            if (value > 1000000000) return (value / 1000000000D).ToString("0.##G");
            if (value > 100000000) return (value / 1000000D).ToString("0.#M");
            if (value > 1000000) return (value / 1000000D).ToString("0.##M");
            if (value > 100000) return (value / 1000D).ToString("0.#k");
            if (value > 10000) return (value / 1000D).ToString("0.##k");

            return value.ToString("#,0");
        }

#endregion
    }
}
