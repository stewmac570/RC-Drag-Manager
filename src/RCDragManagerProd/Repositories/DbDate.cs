using System;
using System.Globalization;

namespace RCDragManagerProd.Repositories
{
    /// <summary>
    /// Formats and parses the 'yyyy-MM-dd HH:mm:ss' date strings stored in the
    /// EventDate columns. Both directions use the invariant culture: custom format
    /// strings substitute the current culture's date/time separators (e.g. fi-FI
    /// writes "18.30.00"), which breaks SQLite's datetime() ordering and re-parsing
    /// under a different culture (#382).
    /// </summary>
    public static class DbDate
    {
        private const string StorageFormat = "yyyy-MM-dd HH:mm:ss";

        public static string ToDbString(DateTime value) =>
            value.ToString(StorageFormat, CultureInfo.InvariantCulture);

        /// <summary>Parses a stored EventDate; returns DateTime.MinValue instead of
        /// throwing so one malformed row can never break a list screen.</summary>
        public static DateTime ParseOrMinValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.MinValue;

            if (DateTime.TryParseExact(raw, StorageFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var exact))
                return exact;

            // Legacy rows may have been written with culture-specific separators.
            if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out var legacy))
                return legacy;

            return DateTime.MinValue;
        }
    }
}
