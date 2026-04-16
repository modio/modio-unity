using System.Diagnostics.CodeAnalysis;

namespace Modio.Monetization
{
    public static class PriceFormatter
    {
        /// <summary>
        /// Helps format a price based on currency code.
        /// </summary>
        /// <param name="currency">The currency code.</param>
        /// <param name="price">The price amount.</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        public static string FormatPrice(string currency, double price)
        {
            var str = price.ToString("0.00");

            return currency switch
            {
                "AED" => str + "د.إ",
                "ARS" => "$" + str + " ARS",
                "AUD" => "A$" + str,
                "BRL" => "R$" + str,
                "CAD" => "C$" + str,
                "CHF" => "Fr. " + str,
                "CLP" => "$" + str + " CLP",
                "CNY" => str + "元",
                "COP" => "COL$ " + str,
                "CRC" => "₡" + str,
                "EUR" => "€" + str,
                "GBP" => "£" + str,
                "HKD" => "HK$" + str,
                "IDR" => "Rp" + str,
                "ILS" => "₪" + str,
                "INR" => "₹" + str,
                "JPY" => "¥" + str,
                "KRW" => "₩" + str,
                "KWD" => "KD " + str,
                "KZT" => str + "₸",
                "MXN" => "Mex$" + str,
                "MYR" => "RM " + str,
                "NOK" => str + " kr",
                "NZD" => "$" + str + " NZD",
                "PEN" => "S/. " + str,
                "PHP" => "₱" + str,
                "PLN" => str + "zł",
                "QAR" => "QR " + str,
                "RUB" => str + "₽",
                "SAR" => "SR " + str,
                "SEK" => str + "kr",
                "SGD" => "S$" + str,
                "THB" => "฿" + str,
                "TRY" => "₺" + str,
                "TWD" => "NT$ " + str,
                "UAH" => "₴" + str,
                "USD" => "$" + str,
                "UYU" => "$U " + str,
                "VND" => "₫" + str,
                "ZAR" => "R " + str,
                _     => str + " " + currency,
            };
        }
    }
}