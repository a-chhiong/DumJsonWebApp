using System.Globalization;
using CrossCutting.Logger;

namespace CrossCutting.Helpers;

public static class DateTimeHelper
{   
    public static DateTime? Create(string? year, string? month, string? day)
    {
        if (int.TryParse(year, out int y) 
            && int.TryParse(month, out int m) 
            && int.TryParse(day, out int d)
            )
        {
            try
            {
                return new DateTime(y, m, d);
            }
            catch
            {
                AppLog.Error($"{year}, {month}, {day}");
                return null;   // invalid date such as 2025-02-30
            }
        }

        return null;
    }
    
    public static DateTime? Create(string? year, string? month, string? day, string? hour, string? minute)
    {
        if (int.TryParse(year, out int y) 
            && int.TryParse(month, out int m)
            && int.TryParse(day, out int d)
            && int.TryParse(hour, out int hr)
            && int.TryParse(minute, out int min)
            )
        {
            try
            {
                return new DateTime(y, m, d, hr,min, 0);
            }
            catch
            {
                AppLog.Error($"{year}, {month}, {day}");
                return null;   // invalid date such as 2025-02-30
            }
        }

        return null;
    }

    public static DateTime? Create(string? value, string? format)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(format))
            return null;

        if (DateTime.TryParseExact(
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