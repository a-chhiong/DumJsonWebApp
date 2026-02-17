using System.Globalization;
using CrossCutting.Logger;

namespace CrossCutting.Helpers;

public static class DateOnlyHelper
{
    /// <summary>
    /// Converts string year/month/day into DateOnly safely.
    /// Returns null for invalid numbers or impossible dates.
    /// </summary>
    public static DateOnly? Create(string? year, string? month, string? day)
    {
        if (int.TryParse(year, out int y) &&
            int.TryParse(month, out int m) &&
            int.TryParse(day, out int d))
        {
            try
            {
                return new DateOnly(y, m, d);
            }
            catch
            {
                // Invalid date (e.g. 2025-02-30)
                AppLog.Error($"{year}, {month}, {day}");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a formatted string into DateOnly using the given format.
    /// Returns null if parsing fails.
    /// </summary>
    public static DateOnly? Create(string? value, string? format)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(format))
            return null;

        if (DateOnly.TryParseExact(
                value,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result;
        }

        return null;
    }
}