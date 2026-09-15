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

            if (DateTime.TryParseExact(
                    text,
                    supportedFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out date)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
                return true;

            // Parse pre-formatted Thai month name strings like "22 กรกฎาคม 1992"
            // where the year is ค.ศ. (Gregorian). Must handle BEFORE th-TH culture parse
            // because th-TH (BuddhistCalendar) would misinterpret the year as พ.ศ.
            var thaiMonths = new[] { "มกราคม","กุมภาพันธ์","มีนาคม","เมษายน","พฤษภาคม","มิถุนายน","กรกฎาคม","สิงหาคม","กันยายน","ตุลาคม","พฤศจิกายน","ธันวาคม" };
            for (var m = 0; m < thaiMonths.Length; m++)
            {
                if (!text.Contains(thaiMonths[m])) continue;
                var replaced = text.Replace(thaiMonths[m], (m + 1).ToString("D2"));
                if (DateTime.TryParseExact(replaced.Trim(), new[] { "d MM yyyy", "dd MM yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
                    return true;
                break;
            }

            return DateTime.TryParse(text, ThaiCulture, DateTimeStyles.AllowWhiteSpaces, out date);
        }

        private static int GetBuddhistYear(DateTime date)
        {
            return date.Year >= 2400 ? date.Year : date.Year + 543;
        }

    }
}
