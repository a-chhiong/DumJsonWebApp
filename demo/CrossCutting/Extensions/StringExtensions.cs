using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CrossCutting.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Extracts all digits from a string, ignoring non-digit characters.
    /// Example: "Tel: +886-912345678/ZH" → "886912345678"
    /// </summary>
    public static string DigitsOnly(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return Regex.Replace(input, @"\D", "");
    }
    
    public static string EncodeEmailForAmadeus(this string input)
    {
        return input
            .Replace("@", "//")
            .Replace("-", "./")
            .Replace("_", "..");
    }
    
    public static string DecodeEmailFromAmadeus(this string input)
    {
        return input
            .Replace("//", "@")
            .Replace("./", "-")
            .Replace("..", "_");
    }
    
    public static bool IsDigitsOnly(this string? input)
    {
        return !string.IsNullOrEmpty(input) && input.All(char.IsDigit);
    }

    public static bool IsValidEmail(this string? input)
    {
        return !string.IsNullOrWhiteSpace(input) && new EmailAddressAttribute().IsValid(input);
    }
    
    /// <summary>
    /// Safely parses a string to int?, returns null if parsing fails.
    /// </summary>
    public static int? ToNullableInt(this string? input)
    {
        return int.TryParse(input, out var value) ? value : (int?)null;
    }

    /// <summary>
    /// Safely parses a string to int, returns defaultValue if parsing fails.
    /// </summary>
    public static int ToIntOrDefault(this string? input, int defaultValue = 0)
    {
        return int.TryParse(input, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Safely parses a string to decimal?, returns null if parsing fails.
    /// </summary>
    public static decimal? ToNullableDecimal(this string? input)
    {
        return decimal.TryParse(input, out var value) ? value : (decimal?)null;
    }
    
    /// <summary>
    /// Adds "TW" prefix if not already present.
    /// </summary>
    public static string AddPrefixTW(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.StartsWith("TW") ? input : $"TW{input}";
    }

    /// <summary>
    /// Removes "TW" prefix if present.
    /// </summary>
    public static string RemovePrefixTW(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.StartsWith("TW") ? input.Substring(2) : input;
    }

    public static string SafeFirst(this string input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Length >= length
            ? input.Substring(0, length)
            : input; // no padding
    }

    public static string SafeFirst(this string input, int length, char pad)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Length >= length
            ? input.Substring(0, length)
            : input.PadRight(length, pad); // with padding
    }
}