using System.Globalization;
using CrossCutting.Constants;

namespace CrossCutting.Helpers;

public static class TimeOnlyHelper
{
    public static TimeOnly? Create(string? value, string? format)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(format))
            return null;

        if (TimeOnly.TryParseExact(
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