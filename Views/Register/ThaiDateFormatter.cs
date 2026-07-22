using System.Globalization;

namespace JobOnlineAPI.Views.Register
{
    internal static class ThaiDateFormatter
    {
        private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

        public static string FormatFull(object? value)
        {
            return TryGetDate(value, out var date)
                ? $"{date.Day} {ThaiCulture.DateTimeFormat.GetMonthName(date.Month)} {GetBuddhistYear(date)}"
                : "";
        }

        public static string FormatMonthYear(object? value)
        {
            return TryGetDate(value, out var date)
                ? $"{ThaiCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month)} {GetBuddhistYear(date)}"
                : "";
        }

        private static bool TryGetDate(object? value, out DateTime date)
        {
            if (value is DateTime dateTime)
            {
                date = dateTime;
                return true;
            }

            if (value is null || value == DBNull.Value)
            {
                date = default;
                return false;
            }

            var text = value.ToString();
            var supportedFormats = new[]
            {
                "dd/MM/yyyy",
                "d/M/yyyy",
                "yyyy-MM-dd",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFFFFFF"
            };

            return DateTime.TryParseExact(
                       text,
                       supportedFormats,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AllowWhiteSpaces,
                       out date)
                   || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)
                   || DateTime.TryParse(text, ThaiCulture, DateTimeStyles.AllowWhiteSpaces, out date);
        }

        private static int GetBuddhistYear(DateTime date)
        {
            return date.Year >= 2400 ? date.Year : date.Year + 543;
        }
    }
}
