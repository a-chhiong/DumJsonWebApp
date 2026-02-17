using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace CrossCutting.Extensions;

public static class EnumExtensions
{
    public static int GetInt(this Enum value)
        => Convert.ToInt32(value);
    
    public static string GetString(this Enum value) 
        => value.GetInt().ToString();

    public static string GetMember(this Enum value)
        => value.GetType()
            .GetMember(value.ToString()).FirstOrDefault()
            ?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? value.ToString();
    
    public static string GetName(this Enum value)
        => value.GetType()
            .GetField(value.ToString())
            ?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static string GetDescription(this Enum value)
        => value.GetType()
            .GetField(value.ToString())
            ?.GetCustomAttribute<DisplayAttribute>()?.Description ?? value.ToString();
}