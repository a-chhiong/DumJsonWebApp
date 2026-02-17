using System.Globalization;

namespace CrossCutting.Helpers;

public static class CultureInfoHelper
{
    public static bool IsValidCulture(string? token, out CultureInfo? culture)
    {
        culture = null;
        
        if (string.IsNullOrWhiteSpace(token))
            return false;
        
        if (!ValidCultureNames.Contains(token, StringComparer.OrdinalIgnoreCase)
            && !ValidCultureTwoLetters.Contains(token, StringComparer.OrdinalIgnoreCase)
            && !ValidCultureThreeLettersIso.Contains(token, StringComparer.OrdinalIgnoreCase)
            && !ValidCultureThreeLettersWin.Contains(token, StringComparer.OrdinalIgnoreCase)) return false;
        
        culture = new CultureInfo(token);
        
        return true;
    }
    
    private static readonly HashSet<string> ValidCultureNames =
        CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    
    private static readonly HashSet<string> ValidCultureTwoLetters =
        CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(c => c.TwoLetterISOLanguageName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    
    private static readonly HashSet<string> ValidCultureThreeLettersIso =
        CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(c => c.ThreeLetterISOLanguageName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ValidCultureThreeLettersWin =
        CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(c => c.ThreeLetterWindowsLanguageName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}